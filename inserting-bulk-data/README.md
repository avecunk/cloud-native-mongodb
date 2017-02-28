# Importing large datasets

## Importing programmatically via JavaScript

### Start a fresh mongodb with a volume on the host

    $ docker run --name insert-demo -m 4G -v /data/db -d mongo

### Download sample data

There's a free sample data set at: https://labrosa.ee.columbia.edu/millionsong/lastfm

    $ wget http://labrosa.ee.columbia.edu/millionsong/sites/default/files/lastfm/lastfm_subset.zip
    $ unzip lastfm_subset.zip

### Examine the data

    $ cat lastfm_subset/A/B/C/TRABCAJ12903CDFCC2.json

    {"artist": "Luna Orbit Project", "timestamp": "2011-08-15 12:59:39.620962", "similars": [], 
    "tags": [], "track_id": "TRABCAJ12903CDFCC2", "title": "Midnight star"}

### Launch a mongo shell with the data mounted in

    $ docker run --rm -it -v `pwd`/lastfm_subset:/lastfm --link insert-demo:mongodb mongo mongo mongodb:27017

    > listFiles('/lastfm')
    [
        {
            "name" : "/lastfm/A",
            "baseName" : "A",
            "isDirectory" : true
        },
        {
            "name" : "/lastfm/B",
            "baseName" : "B",
            "isDirectory" : true
        }
    ]

### Create a collection and helper function

    > db.createCollection('lastfm')
    { "ok" : 1 }

    # small timer so we can compare different methods
    function time() {
        let params = Array.prototype.slice.call(arguments), func = params.shift();
        let start = new Date().getTime();
        func.apply(this, params);
        let finish = new Date().getTime();
        print((finish - start) / 1000, "seconds")
    }


### Write helper functions to import the data

A naive JSON import:

    function importOneFile(file, collection) {
        var json = JSON.parse(cat(file));
        collection.insertOne(json);
    }


Recursively import files:

    function importDirNaive(dir, collection) {
        let files = listFiles(dir);
        for (entry of files) { 
            if (entry.isDirectory) {
                importDirNaive(entry.name, collection);
            } else {
                importOneFile(entry.name, collection);
            }
        }
    }

### Try to import the data

    > time(importDirNaive, "/lastfm", db.lastfm)
    48.755 seconds

    > db.lastfm.count()
    9330

    > db.lastfm.drop()

### Lets do it in chunks of 1000

    function importManyFiles(files, collection) {
        print("importing", files.length, "files");
        let json = files.map(function(file) { return JSON.parse(cat(file)) });
        collection.insertMany(json);
    }

    // we collect chunks of 1000 files to import 
    function importDirFaster(dir, collection, buffer, first) {
        let files = listFiles(dir);
        for (entry of files) { 
            if (entry.isDirectory) {
                importDirFaster(entry.name, collection, buffer, false);
            } else {
                buffer.push(entry.name);
                if (buffer.length >= 1000) {
                    importManyFiles(buffer, collection);
                    buffer.length = 0;
                }
            }
        }
        if (first && buffer.length > 0) {
            importManyFiles(buffer, collection);
        }
    }

    > time(importDirFaster, "/lastfm", db.lastfm, [], true)
    42.086 seconds

    > db.lastfm.drop()

#### insertMany() vs. insert()

`insertMany()` returns a differnent result than the older `insert()` method, which is also a bit more useful for scripting:

    {
        "acknowledged" : true,
        "insertedIds" : [
            ObjectId("fce5728aad85fce5728aad85"),
            ObjectId("76610ec20bf576610ec20bf5"),
            ObjectId("14d71861533614d718615336")
        ]
    }    

### Trying with unordered bulk operation and relaxed write concern

Bulk Writes allow you to combine different write operations (e.g. updates, deleted, inserts) on a single collection.
No round trips to application code required between individual operations.

    function importFileBulk(file, bulk) {
        var json = JSON.parse(cat(file));
        // pre-generate ID to avoid mongo shell copying the object again
        json['_id'] = new ObjectId();
        bulk.insert(json);
    }

    function importDirBulk(dir, collection, bulk, first) {
        let files = listFiles(dir);
        for (entry of files) { 
            if (entry.isDirectory) {
                importDirBulk(entry.name, collection, bulk, false);
            } else {
                importFileBulk(entry.name, bulk)
            }
        }
        if (first) bulk.execute({ w: 0, j: false })
    }

    > time(importDirBulk, "/lastfm", db.lastfm, db.lastfm.initializeUnorderedBulkOp(), true)
    42.518 seconds

    > db.lastfm.count()
    9330

    > db.lastfm.drop()

#### Caveats

* Ordered vs. Unordered: Unordered is mainly faster for situations with sharding as multiple shards can accept write operations simultanously.
* Write Concern: No guarantees that data is actually written with `w: 0` and `j: false`.


### Pre-generating one large file

    $ cd import-scripted
    $ echo -n '[' >bulk.json
    $ find ../lastfm_subset -regex .*.json$ -exec cat {} >>bulk.json \; -exec echo -n ',' >>bulk.json \;
    $ truncate -s -1 bulk.json
    $ echo ']' >>bulk.json

### Import it via a script in one go (import.js)

    $ docker build -t node-mongoose .
    $ docker run  -it --rm --link insert-demo:mongodb -v `pwd`:/import node-mongoose /import/import.js
    3.019 seconds

## Importing with `mongoimport`

Works for CSV, TSV or exports generated via `mongoexport` ("extended JSON").
Unlike MySQL, no fancy options to specify delimiters etc. You need to format your file correctly.

### Important options:

* `--mode insert|upsert|merge` to just append, update or merge documents
* `--upsertFields <field1[,field2]>` to handle duplicates
* `--writeConcern <document>` write concern to use
* `--headerline ` for CSV imports, if the header line container the field names
* `--ignoreBlanks` if blank fields should be part of the document
* `--numInsertionWorkers int` use multiple workers to import in parallel (e.g. # of cores)

### Importing a CSV

We use the website of New York to get some data on their social media usage

    $ wget https://data.cityofnewyork.us/api/views/5b3a-rs48/rows.csv?accessType=DOWNLOAD
    $ docker run --rm -it -v `pwd`:/import --link insert-demo:mongodb mongo bash

    # inside the container
    $ mongoimport --host mongodb:27017 -d test -c newyork --type csv --file /import/NYC_Social_Media_Usage.csv --headerline --ignoreBlanks
    ...
    $ mongo mongodb:27017
    > db.newyork.count()
    > db.newyork.findOne()

### Importing the LastFM collection 

`mongoimport` can handle JSON files that just have an object per line, without needing a full array.

    $ cd ..
    $ find lastfm_subset -regex .*.json$ -exec cat {} >>lastfm.json \;
    $ docker run --rm -it -v `pwd`:/import --link insert-demo:mongodb mongo bash

    $ time mongoimport --host mongodb:27017 -d test -c lastfm --file /import/lastfm.json
    real	0m2.096s

### Adding indexes before or after importing

Open a mongo shell again:

    > var res = db.lastfm.find({artist: /^John/})
    > res.size() // takes a bit

    > db.lastfm.createIndex({track_id: 1})
    > db.lastfm.createIndex({title: 1})
    > db.lastfm.createIndex({article: 1})

    # remove entries, not dropping indexes
    > db.lastfm.remove({})

    # run the import from above again (the large lastfm_train dataset is more interesting here)
    $ time mongoimport --host mongodb:27017 -d test -c lastfm --file /import/lastfm.json

Now with indexes present, the import should be slower

### Compare import speed on a replicated database with different writeConcern settings

    $ cd ../containerpilot-mongodb
    $ docker-compose up -d
    $ docker-compose scale mongodb=3
    $ cd ../inserting-bulk-data
    $ docker run -it --rm --link containerpilotmongodb_mongodb_1:mongodb -v `pwd`:/import mongo bash

    # inside the container: first lax writeConcern
    $ time mongoimport -h mongodb:27017 -d test -c lastfm --file /import/lastfm.json --writeConcern '{w: 0, j: false}'
    real	0m2.578s

    # then strict writeConcern
    $ time mongoimport -h mongodb:27017 -d test -c lastfm --file /import/lastfm.json --writeConcern '{w: 3, j: true}'
    real	0m5.175s

### Try importing yourself with a really large dataset

Download the 1 GB large full training data set from http://labrosa.ee.columbia.edu/millionsong/sites/default/files/lastfm/lastfm_train.zip

### Test the impact of an index on query performance with large datasets

    # still inside the container:
    $ mongo mongodb:27017

    > db.lastfm.count({ artist: /^Spider/ })

This took a noticable amount of time. Now create an index:

    > db.lastfm.createIndex({ artist: 1 })
    # Error!

Seems like the dataset is a bit dirty, so lets remove entries with too long `artist` fields:

    > db.lastfm.remove({ $where: 'this.artist.length > 100' })

And try again:

    > db.lastfm.createIndex({ artist: 1 })

Now, the duration of the `count` operation should not be noticable anymore:

    > db.lastfm.count({ artist: /^Spider/ })

### Clean up

Stop the `insert-demo` container again as it was configured with a lot of memory.

    $ docker stop insert-demo
    $ docker rm insert-demo

## Further import optimizations

Command line options:
* Disable journaling (start `mongod` with `--nojournal`)
* Run `mongoimport` with `--numInsertionWorkers <number-of-cores>`

Sharding considerations:
* Pre-Split the collection, so mongodb doesn't have to re-split and re-distribute chunks during import
* Use unordered inserts so writes can be spread across shards concurrently
* Use non-monothonic increasing shard keys to avoid having all writes on the last shard

See also:
* https://docs.mongodb.com/manual/core/bulk-write-operations/#strategies-for-bulk-inserts-to-a-sharded-collection
* https://docs.mongodb.com/manual/tutorial/split-chunks-in-sharded-cluster/

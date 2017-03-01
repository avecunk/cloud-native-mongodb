# Aggregation

## First prepare a fresh mongodb with some data

    $ docker run --name agg-mongo -d mongo
    $ docker run -it --rm --link agg-mongo:mongodb mongo mongo mongodb:27017

    > db.usedcars.insertMany([
        { make: "BMW", price: 22000, color: "grey" },
        { make: "Ford", price: 15000, color: "red" },
        { make: "BMW", price: 52000, color: "grey" },
        { make: "Ford", price: 31000, color: "red" },
        { make: "Porsche", price: 110000, color: "red" },
        { make: "BMW", price: 59000, color: "blue" },
        { make: "BMW", price: 42000, color: "red" },
        { make: "VW", price: 20000, color: "white" },
        { make: "VW", price: 30000, color: "red" },
        { make: "Ford", price: 69000, color: "white" },
        { make: "VW", price: 27000, color: "blue" },
        { make: "VW", price: 9000, color: "red" },
        { make: "BMW", price: 19000, color: "red" },
        { make: "Porsche", price: 80000, color: "red" },
        { make: "VW", price: 21500, color: "blue" },
        { make: "BMW", price: 62000, color: "grey" }
    ])

## Single Purpose:

Single purpose is representet as specific functions such as `count`, `distinct`, `group`.
MongoDB can perform aggregation operations, such as grouping by a specified key and evaluating a total or a count for each distinct group.

Find out which makes and colors we have on sale:

    > db.usedcars.distinct("make")
    > db.usedcars.distinct("color")

Find out how many BMWs we have on sale:

    > db.usedcars.count({ make: "BMW" })    

## Pipeline-Aggregation

The MongoDB aggregation pipeline consists of stages. Each stage transforms the documents as they pass through the pipeline. 
Pipeline stages do not need to produce one output document for every input document; e.g., some stages may generate new documents or 
filter out documents. Pipeline stages can appear multiple times in the pipeline.

Find out how many cars we have on sale for each make:

    > db.usedcars.aggregate({ $group: { _id: "$make", num_cars: { $sum: 1 } } })

Find out the average price by color for VW cars:

    > db.usedcars.aggregate([
        { $match: { make: "VW" } }, 
        { $group: { _id: "$color", avg_price: { $avg: "$price" } } }
    ])

Find the most expensive by make:

    > db.usedcars.aggregate({ $group: { _id: "$make", max_price: { $max: "$price" } } })
    
### Joins with `$lookup`

MongoDB supports `LEFT OUTER JOIN`s since version 3.2 with the `$lookup` operator with aggregations.

To test it, let's add another collection to join with:

    > db.carvendors.insertMany([
        { name: "BMW", city: "Munich", employees: 120000 },
        { name: "VW", city: "Wolfsburg", employees: 610000 },
        { name: "Porsche", city: "Stuttgart", employees: 24000 },
        { name: "Ford", city: "Dearborn", employees: 200000 }
    ])

We can now join the `usedcars` collection with the `carvendors` collection, much like in a relational database:

    > db.usedcars.aggregate([
        { $lookup: { from: "carvendors", localField: "make", foreignField: "name", as: "vendor" } }
    ])

Using `$project` we can show only fields that are of interest for us:

    > db.usedcars.aggregate([
        { $lookup: { from: "carvendors", localField: "make", foreignField: "name", as: "vendor" } }, 
        { $project: { _id: 0, color: 1, price: 1, "vendor.name": 1, "vendor.city": 1  }}
    ])

But what happens if we have cars with an "unknown" make in the list?

    > db.usedcars.insert({ make: "Rinspeed", price: 220000, color: "purple" })

Execute the query from above again.

The resultset will include the newly added car, too - only without an embedded `carvendor` document!

We can achieve what corresponds roughly to an `LEFT INNER JOIN` by also using the `$match` operator and
restricting it to known makes:

    > db.usedcars.aggregate([
        { $match: { make: { $in: ["BMW", "VW", "Porsche", "Ford"] } } },
        { $lookup: { from: "carvendors", localField: "make", foreignField: "name", as: "vendor" } }
    ])

Or excluding the new make:

    > db.usedcars.aggregate([
        { $match: { make: { $ne: "Rinspeed"} } },
        { $lookup: { from: "carvendors", localField: "make", foreignField: "name", as: "vendor" } }
    ])

### Refining the output

We might want to also sort the resulting collection by price, and limit it to the 3 most expensive ones:

    > db.usedcars.aggregate([
        { $lookup: { from: "carvendors", localField: "make", foreignField: "name", as: "vendor" } },
        { $sort: { price: -1 } },
        { $limit: 3 }
    ])

We might also want to output a random selection of 2 cars - this is possible with `$sample`:

    > db.usedcars.aggregate([
        { $lookup: { from: "carvendors", localField: "make", foreignField: "name", as: "vendor" } },
        { $sample: { size: 2 } }
    ])

If you execute this repeatedly, it should return different cars each time.

### Geo queries

MongoDB supports a wide variety of geospatial queries. To use them, we first need to add some records with location data:

	> db.bars.insertMany([
        { loc: { coordinates: [ -74.008493, 40.725807 ], type: "Point"}, name: "Moes Tavern"},
        { loc: { coordinates: [ -73.978869, 40.766596 ], type: "Point"}, name: "Poppa Joes"},
        { loc: { coordinates: [ -74.135021, 40.636904 ], type: "Point"}, name: "Buddy's Wonder Bar"},
        { loc: { coordinates: [ -73.876876, 40.703885 ], type: "Point"}, name: "The Assembly Bar"},
        { loc: { coordinates: [ -73.985579, 40.768609 ], type: "Point"}, name: "Flaming Moes"},
        { loc: { coordinates: [ -73.962972, 40.763869 ], type: "Point"}, name: "Jackson Hole"},
        { loc: { coordinates: [ -73.985535, 40.730605 ], type: "Point"}, name: "John's Hideout"},
        { loc: { coordinates: [ -73.881183, 40.701775 ], type: "Point"}, name: "Zum Stammtisch"},
        { loc: { coordinates: [ -73.988991, 40.728848 ], type: "Point"}, name: "Grassroot Tavern"},
        { loc: { coordinates: [ -73.987081, 40.752300 ], type: "Point"}, name: "Mr Broadway"}
    ])

In order to make this searchable, an index is required. The `2dsphere` one is the most modern and exact:

    > db.bars.createIndex({ "loc": "2dsphere" })

Now, lets find the bars within 3000 meters around the central park, and return maximum 5 results:

    > var centralPark = [ -73.966845, 40.781158 ]
    > db.bars.aggregate([{
        $geoNear: {
            near: { type: "Point", coordinates: centralPark },
            num: 5,
            distanceField: "distance",
            maxDistance: 3000,
            spherical: true
        }
    }])

## Map-Reduce

In general, map-reduce operations have two phases: a map stage that processes each document, and a reduce phase that combines the output
of the map operation. Optionally, map-reduce can have a finalize stage to make final modifications to the result.

Determine the average price by color via `mapReduce`:

    > db.usedcars.mapReduce(
        function() {
            emit(this.color, this.price);
        },
        function(key, values) {
            return Array.sum(values) / values.length;
        },
        { out: "avg_price_per_color" }
    )

The result was written in a new collection `avg_price_per_color`, so we can just inspect it:

    > db.avg_price_per_color.find()

# MongoDB Basics

## Prepare a fresh mongodb to test with

    $ docker run --name some-mongo -d mongo

    # build a container we use as client:
    $ docker build -t mongo-cmd .

    $ docker run -it --link some-mongo:mongodb --rm mongo-cmd bash

    # inside the container:
    $ mongoimport -h mongodb --db test --collection restaurants --drop --file /sampledata/primer-dataset.json
    $ mongo mongodb:27017/test

## Creating a database using “use” command

Creating a database in MongoDB is as simple as issuing the "use" command. The following example shows how this can be done.

    > use trainingdb

If the command is executed successfully, the following Output will be shown:

    switched to db trainingdb

MongoDB will automatically switch to the database once created


## Creating a collection using insert()

The easiest way to create a collection is to insert a record (which is nothing more then a document consisting of field names and Values) 
into a collection. If the collection does not exist a new one will be created.

The following example shows how this can be done.

    > db.trainingdb.insert(
        {
            "AttendeeID" : 1,
            "AttendeeName" : "Christian"
        }
    )

    > db.trainingdb.find()

Output:

    { "_id" : ObjectId("58ae8cc1d1b7689d4634a1b4"), "AttendeeID" : 1, "AttendeeName" : "Christian" }


## Queries

    > use test

## Query by a Top Level Field

    > db.restaurants.find( { "borough": "Manhattan" } )

## Query by a Field in an Embedded Document

    > db.restaurants.find( { "address.zipcode": "10075" } )

## Query by a Field in an Array

    > db.restaurants.find( { "grades.grade": "B" } )

## Greater Than Operator ($gt)

    > db.restaurants.find( { "grades.score": { $gt: 30 } } )

The result set includes only the matching documents.

## Less Than Operator ($lt)

    > db.restaurants.find( { "grades.score": { $lt: 10 } } )

## Counting

    > db.restaurants.count( { "borough": "Manhattan" } )

## Removing All Documents That Match a Condition

The following operation removes all documents that match the specified condition.

    > db.restaurants.remove( { "borough": "Manhattan" } )

Count again, and you should see `0` results returned.

## Combining Conditions

Logical *AND*: You can specify a AND for a list of query conditions by separating the conditions with a comma in the conditions document.

    > db.restaurants.count( { "cuisine": "American", "address.zipcode": "10460" } )

Logical *OR*: You can specify a OR for a list of query conditions by using the $or query operator.

    > db.restaurants.count(
        { $or: [ { "cuisine": "American" }, { "address.zipcode": "10460" } ] }
    )

## Sorting, limiting, skipping

To specify an order for the result set, append the `sort()` method to the query. 
Pass to `sort()` method a document which contains the field(s) to sort by and the corresponding sort type, 
e.g. 1 for ascending and -1 for descending.

    > db.restaurants.find().sort( { "borough": 1, "address.zipcode": 1 } )

Return only the first 3 by adding a `limit()`.

    > db.restaurants.find().sort( { "borough": 1, "address.zipcode": 1 } ).limit(3)

Return only the first record is also possible with `findOne()`

    > db.restaurants.findOne()

Return only the third by adding a also `skip()`.

    > db.restaurants.find().sort( { "borough": 1, "address.zipcode": 1 } ).limit(1).skip(2)

The order in which you add `sort()`, `skip()` or `limit()` is *not* significant!

## Simple Projections

You often don't want to return all fields in a document, so `find()` take a second parameter as projection settings:

    > db.restaurants.find({}, { name: 1, _id: 0 }).limit(10)

Output:

    { "name" : "Morris Park Bake Shop" }
    { "name" : "Wendy'S" }
    { "name" : "Riviera Caterer" }
    { "name" : "Tov Kosher Kitchen" }
    { "name" : "Brunos On The Boulevard" }
    ...

## Projections with Arrays

More advanced projections are possible with `$elemMatch`, to project documents with array elements matching a condition:

    > db.restaurants.find({}, { _id: 0, name: 1,  grades: { $elemMatch: { score: 10  } } } ).limit(10)

Important: Only the first element in the array will be returned.

To just limit the number of elements to return use `$slice`.
This returns only the first element of the `grades` array in the result set:

    > db.restaurants.find({}, { _id: 0, name: 1,  grades: { $slice: 1 } } ).limit(10)

This returns the last 2 elements:

    > db.restaurants.find({}, { _id: 0, name: 1,  grades: { $slice: -2 } } ).limit(10)

## Views

Views work like you'd expect in a relational database. You can use them to create permanent projects on complex documents.

First add some data in a new database:

    > use financial

    > db.employee.insert({FirstName : 'John', LastName:  'Test', position : 'CFO', wage : 180000.00 })
    > db.employee.insert({FirstName : 'John', LastName:  'Another Test', position : 'CTO', wage : 210000.00 })
    > db.employee.insert({FirstName : 'Johnny', LastName:  'Test', position : 'COO', wage : 180000.00 })

Add a view called `employee_names`:

    > db.createView('employee_names','employee', [{ $project : { _id : 0, "fullname" : {$concat : ["$FirstName", " ", "$LastName"]}}}])

This view also shows up as collection:

    > show collections

And you can look it up programmatically:

    > db.system.views.find()

Otherwise, it behaves much like a normal collection:

    > db.employee_names.find()

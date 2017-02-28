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

## Joins with `$lookup`

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

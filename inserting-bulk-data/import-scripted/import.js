
let mongoose = require('mongoose'),
    fs       = require('fs');

mongoose.connect('mongodb://mongodb:27017/test');

let db = mongoose.connection;
db.on('error', console.error.bind(console, 'connection error:'));

let start = new Date().getTime();

let json = JSON.parse(fs.readFileSync('/import/bulk.json', 'utf8'));

// "fastest" but also most fragile options:
let writeOptions = { writeConcern: { w: 0, j: false }, ordered: false };

db.collection('lastfm').insertMany(json, writeOptions, 
    function() {
        let finish = new Date().getTime();
        console.log("%s seconds", (finish - start) / 1000)
        process.exit();
    }
);

let MongoClient = require('mongodb').MongoClient
  , express = require('express')
  , app = express();


app.use(function(req, res, next) {
    var data='';
    req.setEncoding('utf8');
    req.on('data', function(chunk) { data += chunk; });
    req.on('end', function() { req.body = data; next(); });
});

MongoClient.connect('mongodb://mongodb:27017/test', function(err, db) {
  
  let snaps = db.collection('snaps');
  snaps.createIndex({ visited: 1 }, { expireAfterSeconds: 60 });

  app.get('/', function(req, res) {
    snaps.find().toArray(function(err, docs) {
      if (err) {
        res.send('error');
      } else {
        let messages = docs.map(function(doc) { return doc.message });
        res.send(messages.join("\n-------------------------\n"))
      }
    });
  })

  app.post('/', function(req, res) {
    snaps.insert({ message: req.body, visited: new Date() }, function(err, r) {
      console.log("Received message:", req.body)
      res.send(err ? 'failed' : 'ok');
    });
  });

  app.listen(3000, '0.0.0.0', function () {
    console.log('Snapchatty app listening on port 3000!')
  })
});
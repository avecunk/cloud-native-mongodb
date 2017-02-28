// =======================================================================
// MongoDB connection
var mongoose = require('mongoose');
mongoose.Promise = require('bluebird');

var options = {
  db: { native_parser: true },
  server: { 
      poolSize: 5,
      socketOptions:  { keepAlive: 120 }      
  },
  // Replica set
  //   replset: { 
  //       rs_name: 'myReplicaSetName',
  //       socketOptions = { keepAlive: 120 }
  //     },
  // user: 'myUserName',
  // pass: 'myPassword'
}

// High availability over multiple mongos instances is also supported. 
// Pass a connection string for your mongos instances and set the mongos option to true:
// mongoose.connect('mongodb://mongosA:27501,mongosB:27501', { mongos: true }, cb);

mongoose.connect('mongodb://mongodb:27017/blogs', options);

var db = mongoose.connection;
db.on('error', console.error.bind(console, 'connection error:'));

// =======================================================================

// import blog
var Blog = require('./blog.js')

var blog = new Blog(db);
blog.Insert("MongoDB", "Container Solutions", 
"This is a blog post to show how to use MongoDB with NodeJS", function(data, err) {
    if (err) {
        console.error(err);
    } else {
        console.log(data);        
    }
});

blog.FindByTitle("MongoDB", function(data, err){
    if (err) {
        console.error(err);
    } else {
        console.log(data);        
    }
})




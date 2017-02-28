var mongoose = require('mongoose');
var Schema = mongoose.Schema;

var blogSchema = new Schema({
  title:  { type: [String], index: true },
  author: { type: [String], index: true },
  body:   String,
  comments: [{ body: String, date: Date }],
  date: { type: Date, default: Date.now },
  hidden: Boolean,
  meta: {
    votes: Number,
    favs:  Number
  }
});
var Blog = mongoose.model('Blog', blogSchema);

function Blog(db) {
  this.db = db;
}

// Insert a new blog post
Blog.prototype.Insert = function(title, author, body, callback) {
    this.db.once('open', function() {
        var blogPost = new Blog({
            title: title,
            author: author,
            body: body,
        });

        blogPost.save(blogPost, function(data, err){
            callback(data, err)
        });
    });    
}

// Find a blog via its title
Blog.prototype.FindByTitle = function(title, callback) {
    this.db.once('open', function() {
        Blog.findOne({title: title}, function(data, err){
            callback(data, err)
        });
    });
}

module.exports = Blog
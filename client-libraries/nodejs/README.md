# Using the NodeJS client library

## Examine the application

Files of interest are:

* `index.js`
* `blog.js`

## Run the example

Run a fresh mongodb:

    $ docker run --name nodejs-mongo -d mongo

Run a container with node installed:

    $ docker run -v `pwd`:/opt --link nodejs-mongo:mongodb -it --rm node bash

Run the example from inside the container:

    $ cd /opt
    $ npm start

Exit with Ctrl+C

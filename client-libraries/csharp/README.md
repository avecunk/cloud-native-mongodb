# Using the .NET client library

## Examine the application

There are 3 files of interest:

* `Program.cs`
* `writer.cs`
* `reader.cs`

## Run the example

Run a fresh mongodb:

    $ docker run --name dotnet-mongo -d mongo

Pull the correct .NET image:

    $ docker pull microsoft/dotnet:1.1-sdk-projectjson

Run it:

    $ docker run -v `pwd`:/opt --link dotnet-mongo:mongodb -it --rm microsoft/dotnet:1.1-sdk-projectjson bash
    $ cd /opt
    $ dotnet restore
    $ dotnet run

## About C# MongoDB driver

Driver core provides a number of services that higher-level drivers can utilize either implicitly or explicitly.

### Connection Pooling

Connection pooling is provided for every server that is discovered. There are a number of settings that govern behavior 
ranging from connection lifetimes to the maximum number of connections in the pool.

### Server Monitoring

Each server that is discovered is monitored. By default, this monitoring happens every 10 seconds and consists of 
an `{ ismaster: 1 }` call followed by a `{ buildinfo: 1 }` call. When servers go down, the frequency of these calls will be 
increased to 500 milliseconds. See the Server Discovery and Monitory Specification for more information.

### Server Selection

An API is provided to allow for robust and configurable server selection capabilities. These capabilities align with the 
Server Selection Specification, but are also extensible if additional needs are required.

### Operations

A large number of operations have been implemented for everything from a generic command like “ismaster” to the extremely 
complicated bulk write (“insert”, “update”, and “delete”) commands and presented as instantiatable classes. These classes 
handle version checking the server to ensure that they will function against all versions of the server in which they exist 
as well as ensuring that subsequent correlated operations (such as get more’s for cursors) function correctly.

### Bindings

Bindings glue together server selection and operation execution by influencing how and where operations get executed. 
It would be possible to construct bindings that, for instance, pipeline multiple operations down the same connection or 
ensure that `OP_GETMORE` requests are sent down the same connection as the initial `OP_QUERY`.

### Eventing

The driver provides many events related to server selection, connection pooling, cluster monitoring, command execution, etc...
These events are subscribable to provide solutions such as logging and performance counters.

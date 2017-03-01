# AutoPilot Pattern MongoDB

*A robust and highly-scalable implementation of MongoDB in Docker using the [Autopilot Pattern](http://autopilotpattern.io/)*

## Architecture

A running cluster includes the following components:
- [ContainerPilot](https://www.joyent.com/containerpilot): included in our MongoDB containers to orchestrate bootstrap behavior and coordinate replica joining using keys and checks stored in Consul in the `health`, and `onChange` handlers
- [MongoDB](https://www.mongodb.com/community): we're using MongoDB 3.4 and setting up a [replica set](https://docs.mongodb.com/manual/replication/)
- [Consul](https://www.consul.io/): used to coordinate replication and failover

## Configuration

The main configuration file is in `etc/containerpilot.json` and lists services to manage, their callbacks and health checking 
parameters. Full documentation is on the Joyent website: https://www.joyent.com/containerpilot/docs/configuration.

## Running the cluster

Starting a new cluster is easy once you have [your `_env` file set with the configuration details](#configuration)

    $ docker-compose up -d
    $ docker-compose logs -f

In a few moments you'll have a running MongoDB ready for a replica set. Both the master and replicas are described as a single `docker-compose` service. During startup, [ContainerPilot](http://containerpilot.io) will ask Consul if an existing master has been created. If not, the node will initialize as a new MongoDB replica set and all future nodes will be added to the replica set by the current master. All master election is handled by [MongoDB itself](https://docs.mongodb.com/manual/core/replica-set-elections/) and the result is cached in Consul.

## Playing with the cluster

- Insert some data:

  ```
  $ docker run -it --rm --link containerpilotmongodb_mongodb_2:mongodb mongo mongo mongodb:27017
  pilot:PRIMARY> db.createCollection("burgers")
  pilot:PRIMARY> db.burgers.insert({name: "Big Mac", meat: "beef", tastiness: 5})
  pilot:PRIMARY> db.burgers.insert({name: "Whopper", meat: "beef", tastiness: 7})
  pilot:PRIMARY> db.burgers.insert({name: "Mac Chicken", meat: "chicken", tastiness: 3})
  pilot:PRIMARY> db.burgers.insert({name: "Veggie Burger", meat: "styrofoam", tastiness: 0})
  ```

- Add two more replicas:

  `docker-compose scale mongodb=3`

  The replicas will automatically be added to the *ReplicaSet* on the master and will register themselves in Consul as replicas once they're ready.

- Verify that the documents you have inserted above has been replicated:

  ```
  # Start an interactive console to the SECONDARY
  $ docker run -it --rm --link containerpilotmongodb_mongodb_2:mongodb mongo mongo mongodb:27017
  pilot:SECONDARY> rs.slaveOk()
  pilot:SECONDARY> db.burgers.find({ tastiness: { $gt: 3 } })
  ```

- Now switch the `PRIMARY` to the last replica:

  ```
  # See in the logs what containerpilot does
  $ docker-compose logs -f | grep manage.py

  # Start an interactive console to the PRIMARY
  $ docker run -it --rm --link containerpilotmongodb_mongodb_1:mongodb mongo mongo mongodb:27017
  pilot:PRIMARY> rs.stepDown()
  $ docker run -it --rm --link containerpilotmongodb_mongodb_2:mongodb mongo mongo mongodb:27017
  pilot:PRIMARY> rs.stepDown()
  pilot:PRIMARY> rs.status()
  ```

- Remove the replica added last to force a new leader election:
  
  ```
  $ docker-compose scale mongodb=2
  $ docker run -it --rm --link containerpilotmongodb_mongodb_2:mongodb mongo mongo mongodb:27017
  pilot:PRIMARY> rs.status()
  ```
  You should now see that the second replica became `PRIMARY`.


### Example session:

```
mongodb_1  | 2017/02/23 17:17:49 2017-02-23 17:17:49,511 INFO manage.py updating replica config in mongo from consul info
mongodb_1  | 2017/02/23 17:18:09 2017-02-23 17:18:09,511 INFO manage.py updating replica config in mongo from consul info
mongodb_3  | 2017/02/23 17:18:50 2017-02-23 17:18:50,362 INFO manage.py stepping down as PRIMARY before shutting down 172.17.0.4
mongodb_3  | 2017/02/23 17:18:50 2017-02-23 17:18:50,363 INFO manage.py waiting for new primary to get elected
mongodb_3  | 2017/02/23 17:18:50 2017-02-23 17:18:50,870 INFO manage.py Mongo or specified replica set not yet available on 172.17.0.4; retrying...
mongodb_3  | 2017/02/23 17:18:52 2017-02-23 17:18:52,379 INFO manage.py Mongo or specified replica set not yet available on 172.17.0.4; retrying...
mongodb_3  | 2017/02/23 17:18:53 2017-02-23 17:18:53,891 INFO manage.py Mongo or specified replica set not yet available on 172.17.0.4; retrying...
mongodb_3  | 2017/02/23 17:18:55 2017-02-23 17:18:55,404 INFO manage.py Mongo or specified replica set not yet available on 172.17.0.4; retrying...
mongodb_3  | 2017/02/23 17:18:56 2017-02-23 17:18:56,915 INFO manage.py Mongo or specified replica set not yet available on 172.17.0.4; retrying...
mongodb_3  | 2017/02/23 17:18:58 2017-02-23 17:18:58,440 INFO manage.py primary elected: (u'172.17.0.5', 27017)
```

You can also browse the Consul UI to look at how ContainerPilot maintains state:

  Set up an SSH tunnel.
  ```
  ssh -L 8500:localhost:8500 workshop@<ip-of-your-machine>
  ```
  Browse to: http://localhost:8500/ui/#/dc1/services


## Current limitations

- Removing another primary now will break the setup: The current implementation of `manage.py` can't reliably clean-up stale replicas when there's no primary anymore.
- Data backups / snapshots are not implemented, yet.

## Advanced Configuration

Pass these variables via an `_env` file.

- `LOG_LEVEL`: control the amount of logging from ContainerPilot
- when the primary node is sent a `SIGTERM` it will [step down](https://docs.mongodb.com/manual/reference/command/replSetStepDown/) as primary; the following control those timeouts
  - `MONGO_SECONDARY_CATCHUP_PERIOD`: the number of seconds that the mongod will wait for an electable secondary to catch up to the primary
  - `MONGO_STEPDOWN_TIME`: the number of seconds to step down the primary, during which time the stepdown member is ineligible for becoming primary
  - `MONGO_ELECTION_TIMEOUT`: after the primary steps down, the amount a tries to check that a new primary has been elected before the node shuts down
- `CONSUL` (optional): defaults to `consul` (and thus use the DNS provided by Docker).

## Credits

Based on https://github.com/autopilotpattern/mongodb, which was sponsored by [Joyent](https://www.joyent.com).

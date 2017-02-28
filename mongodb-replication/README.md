# MongoDB replication

This excercise generates a 3-node MongoDB cluster.

## Prepare the demo

Create data folders for the replicas:

    $ ./init.sh

Install `docker-compose`:

    $ sudo apt-get install docker-compose -y

## Initialize the replica set

    $ docker-compose up -d

Examine the `docker-compose.yml` file to see how things are set up.

## Login to the mongo container started to execute commands

Exec into a container that has access to the cluster, we use the image we built during the `mongodb-basics` exercise:

    $ docker exec -it mongo-cmd /bin/bash

Import some data

    $ mongoimport -h mongo1 --db test --collection restaurants --drop --file /sampledata/primer-dataset.json

Open a mongo shell
    
    $ mongo mongo1:27017

Check the status of the replica-set:

    > rs.status()

Repeat for the secondaries, reachable via the host names `mongo2` and `mongo3`.

### Viewing the cluster logs

To see follow the logs of the cluster, exit the mongo shell and type:

    $ docker-compose logs

Or, for the logs of only a specific container:

    $ docker-compose logs mongo1

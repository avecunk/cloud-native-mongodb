# MongoDB Operations 

For this excercise, we keep using the replicated setup from `mongodb-replication`.

## Run mongostat and mongotop

`mongostat` displays what a mongodb server is doing right now:

    $ docker exec -it mongo-cmd /bin/bash

    # inside the container
    $ mongostat -h mongo1

You should see output like this

    insert query update delete getmore command dirty used flushes vsize   res qrw arw net_in net_out conn set repl                time
        *0    *0     *0     *0       0     1|0  0.0% 0.0%       0  765M 60.0M 0|0 0|0   157b   45.4k    8  rs  PRI Feb 28 14:59:56.201
        *0    *0     *0     *0       0     4|0  0.0% 0.0%       0  765M 60.0M 0|0 0|0   472b   46.4k    8  rs  PRI Feb 28 14:59:57.201
        *0    *0     *0     *0       0     2|0  0.0% 0.0%       0  765M 60.0M 0|0 0|0   158b   45.6k    8  rs  PRI Feb 28 14:59:58.201

`mongotop` displays live statistics for the usage of collections:

    $ mongotop -h mongo1

You see output like this:

    2017-02-28T15:02:00.252+0000	connected to: mongo1

                        ns    total    read    write    2017-02-28T15:02:01Z
            local.oplog.rs      4ms     4ms      0ms
        admin.system.roles      0ms     0ms      0ms
        ...

Of course nothing is happening at the moment.
So leave `mongotop` or `mongostat` running, and do the next excercises in a second window to see the effects of backup and recovery.

## Backup & Restore

Backup all databases using mongodump:
    
    $ docker run -it --link mongo1:mongo -v `pwd`/data:/data/backup --rm mongo sh -c 'mongodump --host mongo1 --port 27017 --out /data/backup/ -v'

Backup a specific collection using mongodump:

    $ docker run -it --link mongo1:mongo -v `pwd`/data:/data/backup --rm mongo sh -c 'mongodump --host mongo1 --port 27017 --collection restaurants --db test --out /data/backup/'

Restore all databases using mongorestore:

    $ docker run -it --link mongo1:mongo -v `pwd`/data:/data/backup --rm mongo sh -c 'mongorestore --host mongo1 --port 27017 /data/backup/'

## Monitoring with Prometheus

Run the mongo exporter for Prometheus.

    $ docker run -d --name mongoexporter --link mongo1:mongo_exporter -e "MONGODB_URI=mongodb://mongo1:27017" -p 9104:9104  muellermich/mongo_exporter

Start prometheus:

    $ docker run -d --name prom --link mongoexporter:prom -p 9090:9090 -v `pwd`/prometheus.yml:/etc/prometheus/prometheus.yml prom/prometheus

Start Grafana:

    $ docker run -d -p 3000:3000 grafana/grafana

* Open Grafana at `http://<ip-of-your-cloud-vm>:3000`.
* Then create a new DataSource called `Prometheus` of type `Prometheus` with URL: `http://<ip-of-your-cloud-vm>:9090` and connection type `Direct`.
* Import the two dashboards `MongoDB-Overview.json` and `MongoDB-WIredTiger.json` from the `dashboards/` directory.

## Example of what you should see (although much simpler...)

    $ http://pmmdemo.percona.com/graph/dashboard/db/pmm-demo

## Tear down the demo again

    $ docker-compose stop


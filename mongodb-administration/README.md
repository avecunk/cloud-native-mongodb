# MongoDB Operations 

For this excercise, we keep using the replicated setup from `mongodb-replication`.

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


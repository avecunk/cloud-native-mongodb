# Securing your MongoDB

## User Authentication

... or "how to avoid having to pay for MongoDB backups with Bitcoin"

### Start a fresh mongodb with authentication enabled

    $ docker run --name secure-mongo -d mongo --auth

### Add the admin user

    $ docker exec -it secure-mongo mongo admin

    > db.createUser({ user: 'urs', pwd: 'secretpassword', roles: [{ role: "userAdminAnyDatabase", db: "admin" } ] });

We'll learn further below, what `userAdminAnyDatabase` means.

### Try to connect without authenticating"

    $ docker exec -it secure-mongo mongo

    > show dbs
    # Error!

### Connect with authenticating

The options `-u` / `--username` and `-p` / `--password` are used for standard authentication:

    $ docker run -it --rm --link secure-mongo:mongodb mongo mongo -u urs -p secretpassword --authenticationDatabase admin mongodb:27017

    > show dbs
    admin  0.000GB
    local  0.000GB

Try to add a collection

    > db.createCollection("foobar")
    # Error!

The user's role does not allow that.

### Advanced authentication options

- `--authenticationMechanism` supports `SCRAM-SHA-1` (default), `MONGODB-CR`, `MONGODB-X509` in the community edition

#### Enterprise options

- **Kerberos** (`GSSAPI`) authentication is only available in Enterprise (`--gssapiServiceName`, `--gssapiHostName` options in tools)
- **LDAP** (`PLAIN`) is both available in MongoDB Enterprise *and* in [Percona Server for MongoDB](https://www.percona.com/software/mongo-database/feature-comparison)

### "Localhost exception"

A freshly installed MongoDB is open to the world until the first user has been created from `localhost`!
You can disable this in the config file with:

    setParameter:
      enableLocalhostAuthBypass: false

## Authorization with roles

### Standard roles

You can create own rules, but usually should be ok with the built-in ones:

* `read`: can read all non-system collections, list collections and indexes and get statistics
* `readWrite`: in addition to `read`, can create, modify and drop non-system collections and indexes
* `dbAdmin`: can read system collections and modify some. Can issue administrative commands for databases (repair, reIndex, drop, compact...)
* `userAdmin`: can manage users and their roles. Thus, he can also grant himself all access to the database he administrates! (or the whole cluster in case of the `admin` database)

**Important**: the `dbAdmin` role does not automatically have read-permissions on non-system collections. That way 
you can have admins managing your database without the possibility to read probably sensitive contents.

* `dbOwner`: Combines `dbAdmin`, `readWrite` and `userAdmin`

### Let's create a user that owns a database:

    > db.createUser({ user: "beat", pwd: "secret", roles: [ {role: "dbOwner", db: "test" } ] })
    > exit

Now log in with that user instead to see what he can do:

    $ docker run -it --rm --link secure-mongo:mongodb mongo mongo -u beat -p secret mongodb:27017
  
    > db.createCollection("boats")
    > db.boats.insert({ name: "Esmeralda", masts: 3, sailors: 25 })

This now succeeds

### Advanced roles:

* `backup` and `restore` are special roles, limited roles for these tasks since 3.4
* "Any"-roles that are not scoped to a specific database, but apply to all (non-system): `readAnyDatabase`, `readWriteAnyDatabase`, `dbAdminAnyDatabase`, `userAdminAnyDatabase`
* `root` is the "god role" with (effectively) all permissions
* cluster managment roles: `clusterAdmin`, `clusterManager`, `clusterMonitor`, `hostManager`

### Creating own roles

For special purposes, you can create own roles. Let's create a very limited role that can only update a `cars` collection:

    > db.createCollection("boats")
    > db.createCollection("cars");
    > db.cars.insert({ make: "BMW", color: "blue", price: 35000.00 })
    > db.cars.insert({ make: "VW", color: "read", price: 28000.00 })

    > db.createRole({ 
        role: "carUpdater", 
        privileges: [ { resource: { db: "test", collection: "cars" }, actions: [ "insert", "update"] } ],
        roles: ["read"]
    })

    > db.createUser({ user: 'nick', pwd: 'swordfish', roles: [{ role: "carUpdater", db: "test" } ] });
    > exit

Login as `nick` and try to insert boats and cars:

    $ docker run -it --rm --link secure-mongo:mongodb mongo mongo -u nick -p swordfish mongodb:27017

    > db.boats.insert({name: "Queen Mary", masts: 5, sailors: 80})
    # Error!

    > db.cars.insert({name: "Lada", color: "brown", price: 5000.00})
    WriteResult({ "nInserted" : 1 })

### Changing and modifying roles

* `db.updateRole()` works like `createRole()`, replacing all fields passed ot it
* `db.grantPrivilegesToRole()` adds specific privileges
* `db.revokePrivilegesFromRole()` removes specific privileges
* `db.grantRolesToRole()` adds privileges that the given roles have
* `db.revokeRolesFromRole()` revokes permissions that the given roles have
* `db.dropRole()` drops the passed role
* `db.dropAllRoles()` removes only user-defined roles

## Setting up TLS ("SSL")

Transport encryption via TLS is useful if you connect clients via untrusted networks (e.g. the Internet),
or if you want to enable client certificates for authentication, instead of username/password.

- Use a proper CA for the certificates for maximum security. You can use your own one or a third party one.
- Self-signed certificates are supported, but only guard against eavesdropping, not man-in-the-middle attacks

### Enable TLS with a self-signed certificate

First, we need a certificate:

    $ openssl req -newkey rsa:2048 -new -x509 -days 365 -nodes -out mongodb-cert.crt -keyout mongodb-cert.key
    $ mkdir cert
    $ cat mongodb-cert.key mongodb-cert.crt > cert/mongodb.pem

TLS can be configured via the config file or as command line option:

    $ docker run -v `pwd`/cert:/cert --name ssl-mongo -d mongo --sslMode=requireSSL --sslPEMKeyFile=/cert/mongodb.pem

Try to connect:

    $ docker run -it --rm --link ssl-mongo:mongodb mongo mongo mongodb:27017
    # Error!

Connect via TLS:

    $ docker run -it --rm --link ssl-mongo:mongodb mongo mongo --sslAllowInvalidCertificates --ssl mongodb:27017
    > show dbs

To use a proper Certificate Authority which validates the certificates, you need to pass in the `--sslCAFile` option.

### Using certificates for authentication

This is anebled either via the command line flag `--clusterAuthMode=X509` or the config file.

You need to create a PEM file for each user, and configure the user id as RFC2253 compliant subject, e.g.:

    $ openssl x509 -in pem-file-of-user.pem -inform PEM -subject -nameopt RFC2253

Copy the string after "`subject= `" and use it in the `createUser()` command instead of the username, e.g.:

    > db.getSiblingDB("$external").runCommand({
        createUser: "CN=christian,OU=INI,O=Swisscom,L=Zurich,ST=ZH,C=CH", 
        roles: [{ role: "readWrite", db: "test" }]
    })

To authenticate against the server pass the pem file via `--sslPEMKeyFile` to the `mongo` shell.

See also:
- https://docs.mongodb.com/manual/tutorial/configure-x509-client-authentication/#addx509subjectuser


### Digression: Configuring the PEM file as secret in Kubernetes

Create a single-line base64-encoded string out of the PEM file:

    base64 -w 0 cert/mongodb.pem
    # Mac users can omit the "-w 0"

Create a file called mongodb-secret-pem.yml

    apiVersion: v1
    kind: Secret
    metadata:
      name: mongodb-pem
    type: Opaque
    data: 
      mongodb_pem: <the base64 encoded string pasted from above>

Make it available in the Kubernetes cluster:

    $ kubectl create -f ./mongodb-secret-pem.yaml

Configure a *Pod* to have it mounted in a volume, e.g. `mongodb-pod.yml`:

    apiVersion: v1
    kind: Pod
    metadata:
      name: mongodb
    spec:
      containers:
        name: mongodb
        image: mongo
        volumeMounts:
          name: cert
          mountPath: /opt/cert
          readOnly: true
      volumes:
        name: cert
        secret:
          secretName: mongodb-pem

Run the pod:

    $ kubectl create -f ./mongodb-pod.yml

### Authenticating replicas

If you run replicas across an untrusted network, you might want to 
authenticate them against each other. There are two options:

1. *Keyfiles:* They act as "shared secret" between cluster members and just use `SCRAM-SHA1` authentication, but do not enforce transport encryption
2. *X.509 Certificates:* Each cluster member gets it's own certificate, signed by a common Certificate Authority and authenticates itself that way. This variant enforces transport encryption.

*Keyfiles* are the simplest mechanism. You can just deploy the file on all nodes (e.g. as *Secret* in Kubernetes like shown above) and 
configure the `mongod` deamon with the `--keyFile` option.

## More Security options

### Field-level redaction

Allows dynamically modifying ("redacting") result sets, based on the actual content of the data returned by a query.

It is implemented as a pipeline operator, called `$redact` for aggregations.

See also:
- https://docs.mongodb.com/manual/tutorial/implement-field-level-redaction/

### MongoDB configuration options

- Starting mongod with `--noscripting` prevents server-side scripting (if you don't need `mapReduce` or complex queries with `$where` and `group`)
- Don't use the deprecated HTTP interfaces (`net.http.enabled`, `net.http.JSONPEnabled`, and `net.http.RESTInterfaceEnabled`)
- Don't disable `net.wireObjectCheck` which guards against malformed BSON
- Configure `net.bindIp` to only listed on the interface of a trusted network

See also:
- https://docs.mongodb.com/manual/administration/security-checklist/
- https://docs.mongodb.com/manual/core/security-network/
- https://docs.mongodb.com/manual/core/security-mongodb-configuration/

### Enterprise features

- In MongoDB Enterprise, the extra paranoid can enable [Auditing](https://docs.mongodb.com/manual/tutorial/configure-auditing/)
- Also in Enterprise, encryption at rest is possible with the default *WiredTiger* storage engine, and also for logs

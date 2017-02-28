FROM mongo:3.4

RUN apt-get update \
	&& apt-get install -y \
		python \
		python-dev \
		gcc \
		curl \
		libffi-dev \
		libssl-dev \
	&& rm -rf /var/lib/apt/lists/*

# get Python drivers MongoDB and Consul
RUN curl -Ls -o get-pip.py https://bootstrap.pypa.io/get-pip.py && \
	python get-pip.py && \
	pip install \
		PyMongo==3.4.0 \
		python-Consul==0.7.0 \
		mock==2.0.0

# Add ContainerPilot and set its configuration file path
ENV CONTAINERPILOT_VER 2.7.0
ENV CONTAINERPILOT file:///etc/containerpilot.json
RUN export CONTAINERPILOT_CHECKSUM=687f7d83e031be7f497ffa94b234251270aee75b \
	&& export CONTAINERPILOT_VER=2.7.0 \
	&& curl -Lso /tmp/containerpilot.tar.gz \
		"https://github.com/joyent/containerpilot/releases/download/${CONTAINERPILOT_VER}/containerpilot-${CONTAINERPILOT_VER}.tar.gz" \
	&& echo "${CONTAINERPILOT_CHECKSUM}  /tmp/containerpilot.tar.gz" | sha1sum -c \
	&& tar zxf /tmp/containerpilot.tar.gz -C /usr/local/bin \
	&& rm /tmp/containerpilot.tar.gz

# add stopping timeouts for MongoDB
ENV MONGO_SECONDARY_CATCHUP_PERIOD 8
ENV MONGO_STEPDOWN_TIME 60
ENV MONGO_ELECTION_TIMEOUT 30

# configure ContainerPilot and MySQL
COPY etc/* /etc/
COPY bin/* /usr/local/bin/

# override the parent entrypoint
ENTRYPOINT []

CMD [ \
	"containerpilot", \
	"mongod", \
	"--replSet=workshop" \
]


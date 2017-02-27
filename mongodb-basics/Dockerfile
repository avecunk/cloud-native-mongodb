FROM mongo:3.4.2
RUN apt-get update -y && apt-get install -y wget jq less
RUN mkdir /sampledata
WORKDIR /sampledata
RUN wget -O /sampledata/primer-dataset.json https://raw.githubusercontent.com/mongodb/docs-assets/primer-dataset/primer-dataset.json

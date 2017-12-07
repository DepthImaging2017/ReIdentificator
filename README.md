# ReIdentificator
Reidentify persons with Kinect

## Requirements

  * mongoDB
  * mongoDB driver

## Install mongoDB driver

Run following command in NuGet Package Manager console
```
Install-Package MongoDB.Driver
```

## Run

  * create Folder `repofolder/ReIdentificator/data/db`
  * start mongoDb `path/to/mongod.exe --dbpath path/to/repofolder/ReIdentificator/data/db`
  * Run application
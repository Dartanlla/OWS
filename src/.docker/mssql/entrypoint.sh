#!/bin/bash

# Start the script to create the DB and user
/user/config/initialization.sh &

# Start SQL Server
/opt/mssql/bin/sqlservr
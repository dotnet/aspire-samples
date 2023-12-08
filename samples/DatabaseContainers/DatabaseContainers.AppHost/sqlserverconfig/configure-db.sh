#!/bin/bash

# set -x

# Adapted from: https://github.com/microsoft/mssql-docker/blob/80e2a51d0eb1693f2de014fb26d4a414f5a5add5/linux/preview/examples/mssql-customize/configure-db.sh

# Wait 60 seconds for SQL Server to start up by ensuring that
# calling SQLCMD does not return an error code, which will ensure that sqlcmd is accessible
# and that system and user databases return "0" which means all databases are in an "online" state
# https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-databases-transact-sql?view=sql-server-2017

dbstatus=1
errcode=1
start_time=$SECONDS
end_by=$((start_time + 60))

echo "Starting check for SQL Server start-up at $start_time, will end at $end_by"

while [[ $SECONDS -lt $end_by && ( $errcode -ne 0 || ( -z "$dbstatus" || $dbstatus -ne 0 ) ) ]]; do
    dbstatus="$(/opt/mssql-tools/bin/sqlcmd -h -1 -t 1 -U sa -P "$MSSQL_SA_PASSWORD" -Q "SET NOCOUNT ON; Select SUM(state) from sys.databases")"
    errcode=$?
    sleep 1
done

elapsed_time=$((SECONDS - start_time))
echo "Stopped checking for SQL Server start-up after $elapsed_time seconds (dbstatus=$dbstatus,errcode=$errcode,seconds=$SECONDS)"

if [[ $dbstatus -ne 0 ]] || [[ $errcode -ne 0 ]]; then
    echo "SQL Server took more than 60 seconds to start up or one or more databases are not in an ONLINE state"
    echo "dbstatus = $dbstatus"
    echo "errcode = $errcode"
    exit 1
fi

# Loop through the .sql files in the /docker-entrypoint-initdb.d and execute them with sqlcmd
for f in /docker-entrypoint-initdb.d/*.sql
do
    echo "Processing $f file..."
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d master -i "$f"
done

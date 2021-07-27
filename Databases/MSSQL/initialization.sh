for i in {1..50};
do
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d master -i setup.sql
    if [ $? -eq 0 ]
    then
        break
    else
        sleep 1
    fi
done
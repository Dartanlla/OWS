FROM mcr.microsoft.com/mssql/server:2017-CU24-ubuntu-16.04

RUN mkdir -p /user/config

WORKDIR /user/config

COPY ./.docker/mssql/entrypoint.sh /user/config
COPY ./.docker/mssql/initialization.sh /user/config
COPY ./.docker/mssql/setup.sql /user/config

RUN chmod +x /user/config/initialization.sh

EXPOSE 1433

ENTRYPOINT /bin/bash ./entrypoint.sh
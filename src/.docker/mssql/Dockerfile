FROM mcr.microsoft.com/mssql/server:2022-preview-ubuntu-22.04

USER root

RUN mkdir -p /user/config
RUN mkdir -p /var/opt/mssql/data
WORKDIR /user/config

COPY . /user/config

RUN chmod +x /user/config/entrypoint.sh
RUN chmod +x /user/config/initialization.sh

RUN groupadd --gid 1001 mssql \
    && useradd --uid 1001 --gid 1001 -m mssqluser

RUN chown -R mssqluser:mssql /user/config
RUN chown -R mssqluser:mssql /var/opt/mssql
RUN chown -R mssqluser:mssql /var/opt/mssql/data

USER mssqluser
EXPOSE 1433
ENTRYPOINT ["./entrypoint.sh"]
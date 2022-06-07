FROM mcr.microsoft.com/mssql/server:2017-CU24-ubuntu-16.04

RUN mkdir -p /user/config
WORKDIR /user/config

COPY . /user/config

RUN chmod +x /user/config/entrypoint.sh
RUN chmod +x /user/config/initialization.sh

EXPOSE 1433

ENTRYPOINT ["./entrypoint.sh"]
FROM mariadb:10.5

RUN mkdir -p /docker-entrypoint-initdb.d

COPY setup.sql /docker-entrypoint-initdb.d

EXPOSE 3306

FROM mariadb:10.5

RUN mkdir -p /docker-entrypoint-initdb.d

COPY ./.docker/mysql/setup.sql /docker-entrypoint-initdb.d

EXPOSE 3306

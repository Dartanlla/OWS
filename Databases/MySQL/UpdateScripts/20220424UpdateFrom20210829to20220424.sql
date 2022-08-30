UPDATE OWSVersion SET OWSDBVersion='20220424' WHERE OWSDBVersion IS NOT NULL;

SELECT OWSDBVersion FROM OWSVersion;

ALTER TABLE WorldServers
ADD ZoneServerGUID CHAR(36) NULL;

DELIMITER //

CREATE OR REPLACE PROCEDURE AddOrUpdateWorldServer(_CustomerGUID CHAR(36),
                                                   _ZoneServerGUID CHAR(36),
                                                   _ServerIP VARCHAR(50),
                                                   _MaxNumberOfInstances INT,
                                                   _InternalServerIP VARCHAR(50),
                                                   _StartingMapInstancePort INT
)
BEGIN
    DECLARE _WorldServerID INT;

    SELECT WS.WorldServerID
    INTO _WorldServerID
    FROM WorldServers WS
    WHERE WS.CustomerGUID = _CustomerGUID
      AND WS.ZoneServerGUID = _ZoneServerGUID;

    IF (_WorldServerID > 0) THEN
        UPDATE WorldServers WS
        SET MaxNumberOfInstances    = _MaxNumberOfInstances,
            Port                    = 8081,
            ServerStatus            = 0,
            InternalServerIP        = _InternalServerIP,
            StartingMapInstancePort = _StartingMapInstancePort,
            ZoneServerGUID          = _ZoneServerGUID
        WHERE CustomerGUID = _CustomerGUID
          AND WS.WorldServerID = _WorldServerID
          AND WS.ZoneServerGUID = _ZoneServerGUID;
    ELSE
        INSERT INTO WorldServers (CustomerGUID, ServerIP, MaxNumberOfInstances, Port, ServerStatus, InternalServerIP,
                                  StartingMapInstancePort, ZoneServerGUID)
        VALUES (_CustomerGUID, _ServerIP, _MaxNumberOfInstances, 8081, 0, _InternalServerIP, _StartingMapInstancePort,
                _ZoneServerGUID);
    END IF;
END;

// DELIMITER ;

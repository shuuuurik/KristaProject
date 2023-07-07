CREATE TABLE IF NOT EXISTS my_table 
(
    last_update_from date NOT NULL,
    last_update_to date NOT NULL,
    json_data json NOT NULL,
    CONSTRAINT pk_is PRIMARY KEY (last_update_from,last_update_to)
);

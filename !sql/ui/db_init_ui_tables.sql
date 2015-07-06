CREATE SCHEMA ui;
/********************************** ui."tObjects" *****************************************/
CREATE TABLE ui."tObjects"
(
  id serial PRIMARY KEY,
  "sName" character varying(256) NOT NULL UNIQUE
) 
WITHOUT OIDS;
/********************************** ui."tClasses" *****************************************/
CREATE TABLE ui."tHierarchy"
(
  id serial PRIMARY KEY,
  "idClasses" character varying(256) NOT NULL UNIQUE
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
) 
WITHOUT OIDS;

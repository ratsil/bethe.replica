/********************************** hk."tDictionary" **************************************/
CREATE TABLE hk."tDictionary"
(
  LIKE "tDictionary",
  "idHK" integer NOT NULL,
  FOREIGN KEY ("idHK") REFERENCES hk."tHouseKeeping" (id) MATCH SIMPLE ON UPDATE RESTRICT ON DELETE RESTRICT
)
WITHOUT OIDS;

CREATE TRIGGER "Dictionary"
  BEFORE INSERT OR UPDATE OR DELETE
  ON hk."tDictionary"
  FOR EACH ROW
  EXECUTE PROCEDURE "fDictionary"();
  
CREATE TRIGGER "HKManagement"
  BEFORE INSERT OR UPDATE OR DELETE
  ON hk."tDictionary"
  FOR EACH ROW
  EXECUTE PROCEDURE hk."fManagement"();
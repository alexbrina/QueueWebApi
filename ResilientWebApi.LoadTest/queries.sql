ATTACH 'D:\DV\ABLabs\ResilientWebApi\ResilientWebApi\WorkRequested.sqlite' AS ATTACHED;

SELECT count(1) FROM ATTACHED.WorkRequested wr;

SELECT count(1) FROM WorkCompleted wc;

SELECT count(1) FROM ATTACHED.WorkRequested wr 
 WHERE NOT EXISTS (SELECT 1 FROM WorkCompleted wc WHERE wc.Id = wr.Id);

ATTACH 'D:\DV\ABLabs\QueueWebApi\QueueWebApi\WorkRequested.sqlite' AS ATTACHED;

SELECT count(1) FROM ATTACHED.WorkRequested wr;

SELECT count(1) FROM WorkCompleted wc;

SELECT count(1) FROM ATTACHED.WorkRequested wr 
 WHERE NOT EXISTS (SELECT 1 FROM WorkCompleted wc WHERE wc.Id = wr.Id);

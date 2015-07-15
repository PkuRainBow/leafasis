# Introduction #

Add your content here.


# Details #

---

  1. Convert Points to Mat
```
CvMat * points_f = cvCreateMat( 1, count, CV_32FC2 );

CvMat points_i = cvMat( 1, count, CV_32SC2, points_f->data.ptr );

cvCvtSeqToArray( contour, points_f->data.ptr, CV_WHOLE_SEQ );

cvConvert( &points_i, points_f );`
```
  1. cvPointSeqFromMat
```
CvSeq* cvPointSeqFromMat( int seq_kind, const CvArr* mat,
                          CvContour* contour_header,
                          CvSeqBlock* block );
CvContour header;
CvSeqBlock block;
CvMat* vector = cvCreateMat( 1, 3, CV_32SC2 );

CV_MAT_ELEM( *vector, CvPoint, 0, 0 ) = cvPoint(100,100);
CV_MAT_ELEM( *vector, CvPoint, 0, 1 ) = cvPoint(100,200);
CV_MAT_ELEM( *vector, CvPoint, 0, 2 ) = cvPoint(200,100);

IplImage* img = cvCreateImage( cvSize(300,300), 8, 3 );
cvZero(img);

cvDrawContours( img, cvPointSeqFromMat(CV_SEQ_KIND_CURVE+CV_SEQ_FLAG_CLOSED,
   vector, &header, &block), CV_RGB(255,0,0), CV_RGB(255,0,0), 0, 3, 8, cvPoint(0,0));
```
  1. 使用线段迭代器计算彩色线上象素值的和
```
CvScalar sum_line_pixels( IplImage* image, CvPoint pt1, CvPoint pt2 )
    {
        CvLineIterator iterator;
        int blue_sum = 0, green_sum = 0, red_sum = 0;
        int count = cvInitLineIterator( image, pt1, pt2, &iterator, 8 );

        for( int i = 0; i < count; i++ ){
            blue_sum += iterator.ptr[0];
            green_sum += iterator.ptr[1];
            red_sum += iterator.ptr[2];
            CV_NEXT_LINE_POINT(iterator);

            /* print the pixel coordinates: demonstrates how to calculate the coordinates */
            {
            int offset, x, y;
            /* assume that ROI is not set, otherwise need to take it into account. */
            offset = iterator.ptr - (uchar*)(image->imageData);
            y = offset/image->widthStep;
            x = (offset - y*image->widthStep)/(3*sizeof(uchar) /* size of pixel */);
            printf("(%d,%d)\n", x, y );
            }
        }
        return cvScalar( blue_sum, green_sum, red_sum );
    }
```

---

  1. sql 选指定的行,实现分页
```
select row_number() over(order by cardid) as row_number, * from card 
var result = (from u in context.Users orderby u.Age select u).Skip(skipCount).Take(pageSize);

SELECT TOP 10 [t1].[Id], [t1].[Name], [t1].[Age]
FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY [t0].[Age]) AS [ROW_NUMBER], [t0].[Id], [t0].[Name], [t0].[Age]
    FROM [dbo].[User] AS [t0]
    ) AS [t1]
WHERE [t1].[ROW_NUMBER] > @p0
ORDER BY [t1].[Age]
-- @p0: Input Int32 (Size = 0; Prec = 0; Scale = 0) [10]
-- Context: SqlProvider(Sql2005) Model: AttributedMetaModel Build: 3.5.20706.1

select * ,myid =( select count(cardid) from card as A where a.cardid < B.cardid) + 1
from card as B 

查询数据库连接数
select   count(*)   from   master.dbo.sysprocesses   where   dbid=db_id('shht1')

SELECT * FROM 
[Master].[dbo].[SYSPROCESSES] WHERE [DBID] 
IN 
(
  SELECT 
   [DBID]
  FROM 
   [Master].[dbo].[SYSDATABASES] 
  WHERE 
   NAME='shht1'
)
```

---

  1. shell slice
```
tasklist -fi "imagename eq posserver.exe" | tee -a posserver.txt && date | tee -a porserver.txt
tasklist -fi "imagename eq postest.exe" | wc -l
```
CREATE OR ALTER PROCEDURE [dbo].[sp_SearchInvoice]
    @CustomerId INT = NULL,
    @Status NVARCHAR(50) = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- Get total count first
    SELECT @TotalCount = COUNT(*)
    FROM Invoices i
    INNER JOIN Customers c ON i.CustomerId = c.CustomerId
    WHERE (@CustomerId IS NULL OR i.CustomerId = @CustomerId)
        AND (@Status IS NULL OR i.Status = @Status)
        AND (@FromDate IS NULL OR i.InvoiceDate >= @FromDate)
        AND (@ToDate IS NULL OR i.InvoiceDate <= @ToDate);
    
    -- Get paged results
    SELECT 
        i.InvoiceId,
        i.InvoiceNumber,
        i.CustomerId,
        c.FirstName + ' ' + c.LastName AS CustomerName,
        i.InvoiceDate,
        i.DueDate,
        i.TotalAmount,
        i.Status
    FROM Invoices i
    INNER JOIN Customers c ON i.CustomerId = c.CustomerId
    WHERE (@CustomerId IS NULL OR i.CustomerId = @CustomerId)
        AND (@Status IS NULL OR i.Status = @Status)
        AND (@FromDate IS NULL OR i.InvoiceDate >= @FromDate)
        AND (@ToDate IS NULL OR i.InvoiceDate <= @ToDate)
    ORDER BY i.InvoiceDate DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
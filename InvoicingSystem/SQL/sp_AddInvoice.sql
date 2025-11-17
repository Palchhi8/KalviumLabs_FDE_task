CREATE OR ALTER PROCEDURE sp_AddInvoice
    @CustomerId INT,
    @TaxRate DECIMAL(5,2),
    @Notes NVARCHAR(500) = NULL,
    @InvoiceItemsJson NVARCHAR(MAX),
    @InvoiceId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @InvoiceNumber NVARCHAR(50);
    DECLARE @InvoiceDate DATETIME = GETDATE();
    DECLARE @Subtotal DECIMAL(18,2) = 0;
    DECLARE @TaxAmount DECIMAL(18,2) = 0;
    DECLARE @TotalAmount DECIMAL(18,2) = 0;
    
    -- Generate invoice number
    DECLARE @Counter INT;
    SELECT @Counter = COUNT(*) + 1 FROM Invoices WHERE CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE);
    SET @InvoiceNumber = 'INV-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + FORMAT(@Counter, '0000');
    
    -- Insert invoice
    INSERT INTO Invoices (
        InvoiceNumber, CustomerId, InvoiceDate, TaxRate, Status, Notes, 
        Subtotal, TaxAmount, TotalAmount, CreatedDate, UpdatedDate, EmailSent
    )
    VALUES (
        @InvoiceNumber, @CustomerId, @InvoiceDate, @TaxRate, 'Active', @Notes,
        0, 0, 0, @InvoiceDate, @InvoiceDate, 0
    );
    
    SET @InvoiceId = SCOPE_IDENTITY();
    
    -- Parse and insert invoice items
    INSERT INTO InvoiceItems (
        InvoiceId, ProductName, Description, Quantity, UnitPrice, 
        DiscountPercentage, DiscountAmount, LineTotal, Unit
    )
    SELECT 
        @InvoiceId,
        JSON_VALUE(value, '$.ProductName'),
        JSON_VALUE(value, '$.Description'),
        CAST(JSON_VALUE(value, '$.Quantity') AS DECIMAL(18,2)),
        CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2)),
        CAST(JSON_VALUE(value, '$.DiscountPercentage') AS DECIMAL(5,2)),
        CAST(JSON_VALUE(value, '$.DiscountAmount') AS DECIMAL(18,2)),
        (CAST(JSON_VALUE(value, '$.Quantity') AS DECIMAL(18,2)) * CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2))) 
        - CAST(JSON_VALUE(value, '$.DiscountAmount') AS DECIMAL(18,2)),
        JSON_VALUE(value, '$.Unit')
    FROM OPENJSON(@InvoiceItemsJson);
    
    -- Calculate totals
    SELECT @Subtotal = SUM(LineTotal) FROM InvoiceItems WHERE InvoiceId = @InvoiceId;
    SET @TaxAmount = @Subtotal * (@TaxRate / 100);
    SET @TotalAmount = @Subtotal + @TaxAmount;
    
    -- Update invoice with totals
    UPDATE Invoices 
    SET Subtotal = @Subtotal, TaxAmount = @TaxAmount, TotalAmount = @TotalAmount,
        UpdatedDate = GETDATE()
    WHERE InvoiceId = @InvoiceId;
END
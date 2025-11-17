CREATE OR ALTER PROCEDURE sp_EditInvoice
    @InvoiceId INT,
    @CustomerId INT,
    @TaxRate DECIMAL(5,2),
    @Notes NVARCHAR(500) = NULL,
    @InvoiceItemsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Subtotal DECIMAL(18,2) = 0;
    DECLARE @TaxAmount DECIMAL(18,2) = 0;
    DECLARE @TotalAmount DECIMAL(18,2) = 0;
    
    -- Check if invoice exists and is not voided
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE InvoiceId = @InvoiceId AND Status != 'Voided')
    BEGIN
        RAISERROR('Invoice not found or is voided', 16, 1);
        RETURN;
    END
    
    -- Delete existing invoice items
    DELETE FROM InvoiceItems WHERE InvoiceId = @InvoiceId;
    
    -- Insert new invoice items
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
    
    -- Update invoice
    UPDATE Invoices 
    SET CustomerId = @CustomerId,
        TaxRate = @TaxRate,
        Notes = @Notes,
        Subtotal = @Subtotal,
        TaxAmount = @TaxAmount,
        TotalAmount = @TotalAmount,
        UpdatedDate = GETDATE()
    WHERE InvoiceId = @InvoiceId;
END
CREATE OR ALTER PROCEDURE sp_CalculateTotals
    @InvoiceItemsJson NVARCHAR(MAX),
    @TaxRate DECIMAL(5,2),
    @Subtotal DECIMAL(18,2) OUTPUT,
    @TaxAmount DECIMAL(18,2) OUTPUT,
    @Total DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create temporary table to store calculated items
    CREATE TABLE #TempItems (
        Quantity DECIMAL(18,2),
        UnitPrice DECIMAL(18,2),
        DiscountAmount DECIMAL(18,2),
        LineTotal DECIMAL(18,2)
    );
    
    -- Parse JSON and calculate line totals
    INSERT INTO #TempItems (Quantity, UnitPrice, DiscountAmount, LineTotal)
    SELECT 
        CAST(JSON_VALUE(value, '$.Quantity') AS DECIMAL(18,2)),
        CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2)),
        CAST(JSON_VALUE(value, '$.DiscountAmount') AS DECIMAL(18,2)),
        (CAST(JSON_VALUE(value, '$.Quantity') AS DECIMAL(18,2)) * CAST(JSON_VALUE(value, '$.UnitPrice') AS DECIMAL(18,2))) 
        - CAST(JSON_VALUE(value, '$.DiscountAmount') AS DECIMAL(18,2))
    FROM OPENJSON(@InvoiceItemsJson);
    
    -- Calculate totals
    SELECT @Subtotal = SUM(LineTotal) FROM #TempItems;
    SET @TaxAmount = @Subtotal * (@TaxRate / 100);
    SET @Total = @Subtotal + @TaxAmount;
    
    -- Clean up
    DROP TABLE #TempItems;
END
CREATE OR ALTER PROCEDURE sp_VoidInvoice
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if invoice exists
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE InvoiceId = @InvoiceId)
    BEGIN
        RAISERROR('Invoice not found', 16, 1);
        RETURN;
    END
    
    -- Check if invoice is already voided
    IF EXISTS (SELECT 1 FROM Invoices WHERE InvoiceId = @InvoiceId AND Status = 'Voided')
    BEGIN
        RAISERROR('Invoice is already voided', 16, 1);
        RETURN;
    END
    
    -- Update invoice status to voided
    UPDATE Invoices 
    SET Status = 'Voided',
        UpdatedDate = GETDATE()
    WHERE InvoiceId = @InvoiceId;
    
    SELECT @@ROWCOUNT as RowsAffected;
END
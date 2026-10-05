using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public class LateFeeService
{
    private const decimal FeePerDay = 50m;

    public int CalculateLateDays(Book book)
    {
        if (!book.DueDate.HasValue)
            return 0;

        if (DateTime.Now.Date <= book.DueDate.Value.Date)
            return 0;

        return (DateTime.Now.Date - book.DueDate.Value.Date).Days;
    }

    public decimal CalculateFee(Book book)
    {
        int lateDays = CalculateLateDays(book);

        return lateDays * FeePerDay;
    }
}
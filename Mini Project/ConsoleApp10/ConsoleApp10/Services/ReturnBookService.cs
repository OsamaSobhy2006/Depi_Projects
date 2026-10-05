using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services;

public enum ReturnBookResult
{
    Success,
    NotFound,
    NotBorrowed,
    LateFeeRequired
}

public class ReturnBookService
{
    private readonly BookRepository _bookRepository;
    private readonly LateFeeService _lateFeeService;

    public ReturnBookService(
        BookRepository bookRepository,
        LateFeeService lateFeeService)
    {
        _bookRepository = bookRepository;
        _lateFeeService = lateFeeService;
    }

    public (ReturnBookResult Result, int LateDays, decimal Fee) ReturnBook(int bookId)
    {
        var book = _bookRepository.GetById(bookId);

        if (book == null)
            return (ReturnBookResult.NotFound, 0, 0);

        if (!book.IsBorrowed)
            return (ReturnBookResult.NotBorrowed, 0, 0);

        int lateDays = _lateFeeService.CalculateLateDays(book);
        decimal fee = _lateFeeService.CalculateFee(book);

        if (fee > 0)
            return (ReturnBookResult.LateFeeRequired, lateDays, fee);

        book.IsBorrowed = false;
        book.DueDate = null;

        _bookRepository.Update(book);

        return (ReturnBookResult.Success, 0, 0);
    }
    public bool CompleteReturn(int bookId)
    {
        var book = _bookRepository.GetById(bookId);

        if (book == null)
            return false;

        if (!book.IsBorrowed)
            return false;

        book.IsBorrowed = false;
        book.DueDate = null;

        _bookRepository.Update(book);

        return true;
    }
}
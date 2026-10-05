using LibraryManagementSystem.Models;
using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services;

public class BorrowBookService
{
    private readonly BookRepository _bookRepository;

    public BorrowBookService(BookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public void BorrowBook(int bookId, int duration)
    {
        Book? book = _bookRepository.GetById(bookId);

        if (book == null)
        {
            Console.WriteLine("Book not found.");
            return;
        }

        if (book.IsBorrowed)
        {
            Console.WriteLine("This book is already borrowed.");
            return;
        }

        if (duration <= 0)
        {
            Console.WriteLine("Borrow duration must be greater than 0.");
            return;
        }

        book.IsBorrowed = true;
        book.DueDate = DateTime.Now.AddDays(-duration);

        _bookRepository.Update(book);

        Console.WriteLine($"Book '{book.Title}' borrowed successfully.");
        Console.WriteLine($"Due date: {book.DueDate:dd/MM/yyyy}");
    }
}

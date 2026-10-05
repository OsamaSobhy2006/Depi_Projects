using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem;

public class LibraryMenu
{
    private readonly CreateBookService _createBookService;
    private readonly GetAllBooksService _getAllBooksService;
    private readonly GetBookByTitleService _getBookByTitleService;
    private readonly UpdateBookService _updateBookService;
    private readonly DeleteBookService _deleteBookService;
    private readonly BorrowBookService _borrowBookService;
    private readonly ReturnBookService _returnBookService;

    public LibraryMenu(
        CreateBookService createBookService,
        GetAllBooksService getAllBooksService,
        GetBookByTitleService getBookByTitleService,
        UpdateBookService updateBookService,
        DeleteBookService deleteBookService,
        BorrowBookService borrowBookService,
        ReturnBookService returnBookService)
    {
        _createBookService = createBookService;
        _getAllBooksService = getAllBooksService;
        _getBookByTitleService = getBookByTitleService;
        _updateBookService = updateBookService;
        _deleteBookService = deleteBookService;
        _borrowBookService = borrowBookService;
        _returnBookService = returnBookService;
    }

    public void Show()
    {
        bool isRunning = true;

        while (isRunning)
        {
            Console.Clear();

            Console.WriteLine("================================");
            Console.WriteLine("      LIBRARY MANAGEMENT SYSTEM");
            Console.WriteLine("================================");
            Console.WriteLine("1. Create Book");
            Console.WriteLine("2. Get All Books");
            Console.WriteLine("3. Get Book By Title");
            Console.WriteLine("4. Update Book");
            Console.WriteLine("5. Delete Book");
            Console.WriteLine("6. Borrow Book");
            Console.WriteLine("7. Return Book");
            Console.WriteLine("8. Exit");
            Console.WriteLine("================================");
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();

            Console.Clear();

            switch (choice)
            {
                case "1":
                    CreateBook();
                    break;

                case "2":
                    GetAllBooks();
                    break;

                case "3":
                    GetBookByTitle();
                    break;

                case "4":
                    UpdateBook();
                    break;

                case "5":
                    DeleteBook();
                    break;

                case "6":
                    BorrowBook();
                    break;

                case "7":
                    ReturnBook();
                    break;

                case "8":
                    isRunning = false;
                    Console.WriteLine("Goodbye!");
                    break;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }

            if (isRunning)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to return to the menu...");
                Console.ReadKey();
            }
        }
    }

    private void CreateBook()
    {
        Console.Write("Enter Book Id: ");
        int id = int.Parse(Console.ReadLine()!);

        Console.Write("Enter Book Title: ");
        string title = Console.ReadLine()!;

        Console.Write("Enter Book Author: ");
        string author = Console.ReadLine()!;

        var book = new Book
        {
            Id = id,
            Title = title,
            Author = author,
            IsBorrowed = false
        };

        _createBookService.CreateBook(book);

        Console.WriteLine();
        Console.WriteLine("Book created successfully.");
    }

    private void GetAllBooks()
    {
        var books = _getAllBooksService.GetAllBooks();

        if (books.Count == 0)
        {
            Console.WriteLine("No books found.");
            return;
        }

        foreach (var book in books)
        {
            Console.WriteLine($"Id: {book.Id}");
            Console.WriteLine($"Title: {book.Title}");
            Console.WriteLine($"Author: {book.Author}");
            Console.WriteLine($"Borrowed: {book.IsBorrowed}");
            Console.WriteLine("--------------------------------");
        }
    }

    private void GetBookByTitle()
    {
        Console.Write("Enter Book Title: ");
        string title = Console.ReadLine()!;

        var book = _getBookByTitleService.GetBookByTitle(title);

        if (book == null)
        {
            Console.WriteLine("Book not found.");
            return;
        }

        Console.WriteLine($"Id: {book.Id}");
        Console.WriteLine($"Title: {book.Title}");
        Console.WriteLine($"Author: {book.Author}");
        Console.WriteLine($"Borrowed: {book.IsBorrowed}");
    }

    private void UpdateBook()
    {
        Console.Write("Enter Book Id: ");
        int id = int.Parse(Console.ReadLine()!);

        Console.Write("Enter New Title: ");
        string newTitle = Console.ReadLine() ?? "";

        Console.Write("Enter New Author: ");
        string newAuthor = Console.ReadLine() ?? "";

        var result = _updateBookService.UpdateBook(
            id,
            newTitle,
            newAuthor);

        switch (result)
        {
            case UpdateBookResult.Success:
                Console.WriteLine("Book updated successfully.");
                break;

            case UpdateBookResult.NotFound:
                Console.WriteLine("Book not found.");
                break;
        }
    }

    private void DeleteBook()
    {
        Console.Write("Enter Book Id: ");
        int id = int.Parse(Console.ReadLine()!);

        var result = _deleteBookService.DeleteBook(id);

        switch (result)
        {
            case DeleteBookResult.Success:
                Console.WriteLine("Book deleted successfully.");
                break;

            case DeleteBookResult.NotFound:
                Console.WriteLine("Book not found.");
                break;

            case DeleteBookResult.BorrowedBook:
                Console.WriteLine("Cannot delete a borrowed book.");
                break;
        }
    }

    private void BorrowBook()
    {
        Console.Write("Enter Book Id: ");
        int id = int.Parse(Console.ReadLine()!);

        Console.Write("Enter borrowing duration in days: ");
        int duration = int.Parse(Console.ReadLine()!);

        _borrowBookService.BorrowBook(id, duration);
    }
    private void ReturnBook()
    {
        Console.Write("Enter Book Id: ");
        int bookId = int.Parse(Console.ReadLine()!);

        var result = _returnBookService.ReturnBook(bookId);

        switch (result.Result)
        {
            case ReturnBookResult.Success:
                Console.WriteLine("Book returned successfully.");
                break;

            case ReturnBookResult.NotFound:
                Console.WriteLine("Book not found.");
                break;

            case ReturnBookResult.NotBorrowed:
                Console.WriteLine("This book is not currently borrowed.");
                break;

            case ReturnBookResult.LateFeeRequired:
                Console.WriteLine($"Book is {result.LateDays} day(s) late.");
                Console.WriteLine($"Late fee: {result.Fee} EGP");
                Console.WriteLine("Payment is required before returning the book.");
                break;
        }
    }
}

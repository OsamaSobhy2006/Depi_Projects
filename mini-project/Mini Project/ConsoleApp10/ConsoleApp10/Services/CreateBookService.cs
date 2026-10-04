using LibraryManagementSystem.Models;
using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services;

public class CreateBookService
{
    private readonly BookRepository _bookRepository;

    public CreateBookService(BookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public void CreateBook(Book book)
    {
        _bookRepository.Add(book);
    }
}
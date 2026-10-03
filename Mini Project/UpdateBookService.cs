using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services
{
    public enum UpdateBookResult
    {
        Success,
        NotFound
    }

    public class UpdateBookService
    {
        private readonly BookRepository _bookRepository;

        public UpdateBookService(BookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public UpdateBookResult UpdateBook(int bookId, string newTitle, string newAuthor)
        {
            var book = _bookRepository.GetById(bookId);

            if (book == null)
                return UpdateBookResult.NotFound;


            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                book.Title = newTitle;
            }


            if (!string.IsNullOrWhiteSpace(newAuthor))
            {
                book.Author = newAuthor;
            }

            _bookRepository.Update(book);

            return UpdateBookResult.Success;
        }
    }
}
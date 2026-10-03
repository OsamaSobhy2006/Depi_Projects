using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services
{
    public enum DeleteBookResult
    {
        Success,
        NotFound,
        BorrowedBook
    }

    public class DeleteBookService
    {
        private readonly BookRepository _bookRepository;

        public DeleteBookService(BookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public DeleteBookResult DeleteBook(int bookId)
        {
            var book = _bookRepository.GetById(bookId);

            if (book == null)
                return DeleteBookResult.NotFound;

            if (book.IsBorrowed)
                return DeleteBookResult.BorrowedBook;

            _bookRepository.Delete(book);

            return DeleteBookResult.Success;
        }
    }
}
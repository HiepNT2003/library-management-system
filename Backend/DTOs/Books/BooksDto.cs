namespace Backend.DTOs.Books
{
    public class BookDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public string Category { get; set; }

        public double Rating { get; set; }

        public int AvailableCopies { get; set; }
    }

    public class BorrowRequestDto
    {
        public int BookId { get; set; }
        public string UserId { get; set; }
    }

    public class RatingDto
    {
        public int BookId { get; set; }
        public string UserId { get; set; }
        public int Score { get; set; }
    }

    public class FavoriteDto
    {
        public int BookId { get; set; }
        public string UserId { get; set; }
    }
}
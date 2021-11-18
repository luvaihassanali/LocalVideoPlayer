using System;

namespace LocalVideoPlayer
{
    class Media
    {
        private Movie[] movies;
        private TvShow[] tvShows;

        public Media(int m, int s)
        {
            movies = new Movie[m];
            tvShows = new TvShow[s];
        }

        public Movie[] Movies
        {
            get => movies;
            set => movies = value;
        }

        public TvShow[] TvShows
        {
            get => tvShows;
            set => tvShows = value;
        }

        internal bool Compare(Media prevMedia)
        {
            if (this.movies.Length != prevMedia.movies.Length) return false;
            if (this.tvShows.Length != prevMedia.tvShows.Length) return false;

            for (int i = 0; i < this.movies.Length; i++)
            {
                if (!this.movies[i].Compare(prevMedia.movies[i])) return false;
            }

            for (int i = 0; i < this.tvShows.Length; i++)
            {
                if (!this.tvShows[i].Compare(prevMedia.tvShows[i])) return false;
            }

            return true;
        }

        internal void Ingest(Media prevMedia)
        {
            for (int i = 0; i < prevMedia.Movies.Length; i++)
            {
                for (int j = 0; j < this.movies.Length; j++)
                {
                    if (String.Compare(this.movies[j].Name, prevMedia.movies[i].Name, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
                    {
                        this.movies[j].Name = prevMedia.movies[i].Name;
                        this.movies[j].Overview = prevMedia.movies[i].Overview;
                        this.movies[j].Path = prevMedia.movies[i].Path;
                        this.movies[j].Poster = prevMedia.movies[i].Poster;
                        this.movies[j].Id = prevMedia.movies[i].Id;
                        this.movies[j].Date = prevMedia.movies[i].Date;
                        this.movies[j].Backdrop = prevMedia.movies[i].Backdrop;
                    }
                }
            }

            for (int i = 0; i < prevMedia.TvShows.Length; i++)
            {
                for (int l = 0; l < this.tvShows.Length; l++)
                {
                    if (String.Compare(this.tvShows[l].Name, prevMedia.tvShows[i].Name, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
                    {
                        this.tvShows[l].Name = prevMedia.tvShows[i].Name;
                        this.tvShows[l].Id = prevMedia.tvShows[i].Id;
                        this.tvShows[l].Overview = prevMedia.tvShows[i].Overview;
                        this.tvShows[l].Poster = prevMedia.tvShows[i].Poster;
                        this.tvShows[l].Date = prevMedia.tvShows[i].Date;
                        this.tvShows[l].Backdrop = prevMedia.tvShows[i].Backdrop;

                        for (int j = 0; j < prevMedia.TvShows[i].Seasons.Length; j++)
                        {
                            this.tvShows[l].Seasons[j].Id = prevMedia.TvShows[i].Seasons[j].Id;
                            this.tvShows[l].Seasons[j].Poster = prevMedia.TvShows[i].Seasons[j].Poster;
                            this.tvShows[l].Seasons[j].Date = prevMedia.TvShows[i].Seasons[j].Date;

                            for (int k = 0; k < prevMedia.TvShows[i].Seasons[j].Episodes.Length; k++)
                            {
                                if (this.tvShows[l].Seasons[j].Episodes[k].Name.Equals(prevMedia.TvShows[i].Seasons[j].Episodes[k].Name))
                                {
                                    this.tvShows[l].Seasons[j].Episodes[k].Id = prevMedia.TvShows[i].Seasons[j].Episodes[k].Id;
                                    this.tvShows[l].Seasons[j].Episodes[k].Name = prevMedia.TvShows[i].Seasons[j].Episodes[k].Name;
                                    this.tvShows[l].Seasons[j].Episodes[k].Backdrop = prevMedia.TvShows[i].Seasons[j].Episodes[k].Backdrop;
                                    this.tvShows[l].Seasons[j].Episodes[k].Date = prevMedia.TvShows[i].Seasons[j].Episodes[k].Date;
                                    this.tvShows[l].Seasons[j].Episodes[k].Overview = prevMedia.TvShows[i].Seasons[j].Episodes[k].Overview;
                                    this.tvShows[l].Seasons[j].Episodes[k].Path = prevMedia.TvShows[i].Seasons[j].Episodes[k].Path;
                                }
                            }

                        }
                    }
                }
            }
        }
    }

    public class Movie
    {
        private int id;
        private string name;
        private string path;
        private string poster;
        private string backdrop;
        private string overview;
        DateTime? date;

        public Movie(string n, string p)
        {
            name = n;
            path = p;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }
        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Path
        {
            get => path;
            set => path = value;
        }
        public string Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        public string Poster
        {
            get => poster;
            set => poster = value;
        }

        public string Overview
        {
            get => overview;
            set => overview = value;
        }

        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        internal bool Compare(Movie localMovie)
        {
            if (!this.name.Equals(localMovie.Name)) return false;
            if (!this.path.Equals(localMovie.Path)) return false;

            return true;
        }
    }

    public class TvShow
    {
        private int id;
        private string name;
        private string overview;
        private string backdrop;
        private string poster;
        private Season[] seasons;
        DateTime? date;

        public TvShow(string n)
        {
            name = n;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        public string Poster
        {
            get => poster;
            set => poster = value;
        }

        public string Overview
        {
            get => overview;
            set => overview = value;
        }

        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        public Season[] Seasons
        {
            get => seasons;
            set => seasons = value;
        }

        internal bool Compare(TvShow localShow)
        {
            if (!this.name.Equals(localShow.name)) return false;
            if (this.seasons.Length != localShow.seasons.Length) return false;
            for (int i = 0; i < this.seasons.Length; i++)
            {
                if (!this.seasons[i].Compare(localShow.seasons[i])) return false;
            }
            return true;
        }
    }

    public class Season
    {
        private int id;
        private Episode[] episodes;
        private string poster;
        DateTime? date;

        public Season(int i)
        {
            id = i;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }

        public string Poster
        {
            get => poster;
            set => poster = value;
        }

        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        public Episode[] Episodes
        {
            get => episodes;
            set => episodes = value;
        }

        internal bool Compare(Season localSeason)
        {
            if (this.episodes.Length != localSeason.episodes.Length) return false;

            for (int i = 0; i < this.episodes.Length; i++)
            {
                if (!this.episodes[i].Compare(localSeason.episodes[i])) return false;
            }
            return true;
        }
    }

    public class Episode
    {
        private int id;
        private string name;
        private string backdrop;
        private string overview;
        private string path;
        DateTime? date;

        public Episode(int i, string n, string p)
        {
            id = i;
            name = n;
            path = p;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }
        public string Name
        {
            get => name;
            set => name = value;
        }
        public string Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        public string Overview
        {
            get => overview;
            set => overview = value;
        }
        public DateTime? Date
        {
            get => date;
            set => date = value;
        }

        public string Path
        {
            get => path;
            set => path = value;
        }

        internal bool Compare(Episode otherEpisode)
        {
            if (!this.name.Equals(otherEpisode.name)) return false;
            if (!this.id.Equals(otherEpisode.id)) return false;

            return true;
        }
    }
}

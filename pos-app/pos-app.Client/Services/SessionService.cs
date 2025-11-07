namespace pos_app.Client.Services
{
    /// <summary>
    /// Service to manage session data, particularly the minimum date from session table
    /// </summary>
    public class SessionService
    {
        private readonly DataService _dataService;
        private readonly ILogger<SessionService> _logger;
        private DateTime? _cachedMinDate;
        private bool _isLoaded = false;

        public SessionService(DataService dataService, ILogger<SessionService> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the minimum date from the session table (start_date)
        /// Returns cached value if available, otherwise fetches from API
        /// </summary>
        public async Task<DateTime?> GetMinDateAsync()
        {
            if (_isLoaded)
            {
                return _cachedMinDate;
            }

            try
            {
                _cachedMinDate = await _dataService.GetSessionStartDateAsync();
                _isLoaded = true;

                if (_cachedMinDate.HasValue)
                {
                    _logger.LogInformation("Session start date loaded: {StartDate}", _cachedMinDate.Value);
                }
                else
                {
                    _logger.LogWarning("No session start date found, using default minimum date");
                }

                return _cachedMinDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading session start date");
                _isLoaded = true; // Mark as loaded to prevent repeated failed attempts
                return null;
            }
        }

        /// <summary>
        /// Clears the cached minimum date (useful if session data changes)
        /// </summary>
        public void ClearCache()
        {
            _cachedMinDate = null;
            _isLoaded = false;
        }
    }
}


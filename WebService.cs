[WebMethod]
        public FindCompanyRS FindCompaniesByFullOrTradeName(FindCompanyRQ request) {

            return Process(request, () => {

                List<CompanyModel> results = _crm.FindCompaniesByFullOrTradeName(
                    request.Criteria,
                    new CompanyOutput() { IncludeDetails = true,  IncludeMostImportantFieldDetails = true }
                    );

                if (results.Count == 0) {
                    return new FindCompanyRS() {
                        Success = false,
                        Errors = new List<ErrorRS>() { new ErrorRS() { Code = -1, Message = "No company found" } },

                    };
                }


                return new FindCompanyRS() {
                    Success = true,
                    Results = results,
                };

            });

        }

        private static TRs Process<TRq, TRs>(TRq request, Func<TRs> fCallback, [CallerMemberName] string methodName = null)
            where TRq : RequestBase
            where TRs : ResponseBase, new()
        {


            TRs ret;
            Stopwatch swTimer = Stopwatch.StartNew();
            try {
               // Log2.Debug("Starting " + methodName + "\r\n" + XmlUtils.Serialise(request));
                ret = fCallback();
                swTimer.Stop();
            }
            catch (Exception ex) {
                swTimer.Stop();
                Log2.Error("Exception thrown: " + ex);
                ret = new TRs() {
                    Success = false,
                    Errors = new List<ErrorRS>() {
                         new ErrorRS() { Code = -1, Message = ex.Message }
                     }
                };
            }
            finally {
                Log2.Notice("Finish " + methodName + ", took: " + (1000 * swTimer.ElapsedTicks / Stopwatch.Frequency).ToString("0.0ms"));
                Log2.Debug("Finished web method:" + methodName);
               
                FlushLogs();
            }
            return ret;
        }
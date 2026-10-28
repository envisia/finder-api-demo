namespace FinderDemo.Areas.Search;

/// <summary>The search filters the demo offers; the API can combine all of them in one request.</summary>
internal enum SearchMode
{
    /// <summary><c>name</c>: name of the document folder.</summary>
    Name,

    /// <summary><c>id</c>: EfinGuid of the document folder.</summary>
    EfinGuid,

    /// <summary><c>query</c>: full text of the documents.</summary>
    FullText,

    /// <summary><c>empty_additionals</c>: folders where the chosen additionals are not set.</summary>
    AdditionalNotSet,
}

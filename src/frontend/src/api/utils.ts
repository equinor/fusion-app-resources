type ResourceQueryParams = {
    requestId?: string;
    positionId?: string
};

export const parseQueryString = (queryString: string) => {
    const segments = queryString.replace('?', '').split('&');
    const parsed = segments.reduce((params, segment) => {
        const parts = segment.split('=');
        return {
            ...params,
            [parts[0]]: parts[1],
        };
    }, {});

    return parsed as ResourceQueryParams;
};

export const formatDateToYDMString = (date: Date)=> {
    return new Date(date.getTime() - (date.getTimezoneOffset() * 60000 ))
    .toISOString()
    .split("T")[0]
}
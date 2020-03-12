import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { useHistory } from '@equinor/fusion';

type OrgQueryParams = {
    requestId: string;
};

const parseQueryString = (queryString: string) => {
    const segments = queryString.replace('?', '').split('&');
    const parsed = segments.reduce((params, segment) => {
        const parts = segment.split('=');
        return {
            ...params,
            [parts[0]]: parts[1],
        };
    }, {});

    return parsed as OrgQueryParams;
};

export default (requests: PersonnelRequest[] | null) => {
    const history = useHistory();
    const [currentRequest, setCurrentRequest] = React.useState<PersonnelRequest | null>(null);

    React.useEffect(() => {
        if (!requests) {
            return;
        }
        const params = parseQueryString(history.location.search);
        const selectedRequest = requests.find(r => r.id === params.requestId);
        setCurrentRequest(selectedRequest || null);
    }, [history.location.search, requests]);

    React.useEffect(() => {
        if(currentRequest === null){
            history.push({
                pathname: history.location.pathname,
                search: "",
            });
        }
    },[currentRequest])

    return {currentRequest, setCurrentRequest};
};

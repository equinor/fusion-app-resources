import * as React from 'react';
import {
    ModalSideSheet,
    Spinner,
    Accordion,
    AccordionItem,
    PersonCard,
    DoneIcon,
    ErrorIcon,
    Button,
    IconButton,
    CloseIcon,
} from '@equinor/fusion-components';
import classNames from 'classnames';
import { FailedRequest, SuccessfulRequest } from '../hooks/useSubmitChanges';
import CreatePersonnelRequest from '../../../../../../../models/CreatePersonnelRequest';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import * as styles from '../styles.less';
import { EditRequest } from '..';
import useServiceNowPopoverRef from '../../../../../../../hooks/useServiceNowPopoverRef';

type RequestProgressSidesheetProps = {
    pendingRequests: EditRequest[];
    failedRequests: FailedRequest<EditRequest>[];
    successfulRequests: SuccessfulRequest<EditRequest, PersonnelRequest>[];
    onClose: () => void;
};

type RequestItemProps = {
    request: EditRequest;
};

type FailedRequestItemProps = RequestItemProps & {
    error: Error;
};

type SuccessfulRequestItemProps = RequestItemProps & {
    response: PersonnelRequest;
};

const PendingRequestProgressItem: React.FC<RequestItemProps> = ({ request }) => {
    return (
        <div className={classNames(styles.item, styles.pending)}>
            <div className={styles.icon}>
                <Spinner inline size={24} />
            </div>
            <div className={styles.position}>{request.positionName}</div>
            <div className={styles.person}>
                <PersonCard personId={request.person?.mail} />
            </div>
        </div>
    );
};

const InvalidRequestProgressItem: React.FC<FailedRequestItemProps> = ({ request, error }) => {
    return (
        <div className={classNames(styles.item, styles.failed)}>
            <div className={styles.icon}>
                <IconButton>
                    <CloseIcon />
                </IconButton>
            </div>
            <div className={styles.icon}>
                <ErrorIcon outline={false} />
            </div>
            <div className={styles.position}>{request.positionName}</div>
            <div className={styles.person}>
                <PersonCard personId={request.person?.mail} />
            </div>
            <div className={styles.errorMessage}>{error.message}</div>
        </div>
    );
};

const FailedRequestProgressItem: React.FC<FailedRequestItemProps> = ({ request }) => {
    return (
        <div className={classNames(styles.item, styles.failed)}>
            <div className={styles.icon}>
                <IconButton>
                    <CloseIcon />
                </IconButton>
            </div>
            <div className={styles.icon}>
                <ErrorIcon outline={false} />
            </div>
            <div className={styles.position}>{request.positionName}</div>
            <div className={styles.person}>
                <PersonCard personId={request.person?.mail} />
            </div>
        </div>
    );
};

const SuccesfulRequestProgressItem: React.FC<SuccessfulRequestItemProps> = ({ request }) => {
    return (
        <div className={classNames(styles.item, styles.successful)}>
            <div className={styles.icon}>
                <DoneIcon />
            </div>
            <div className={styles.position}>{request.positionName}</div>
            <div className={styles.person}>
                <PersonCard personId={request.person?.mail} />
            </div>
        </div>
    );
};

const RequestProgressSidesheet: React.FC<RequestProgressSidesheetProps> = ({
    pendingRequests,
    failedRequests,
    successfulRequests,
    onClose,
}) => {
    const [isPendingRequestsOpen, setIsPendingRequestsOpen] = React.useState(true);
    const [isSuccessfulRequestsOpen, setIsSuccessfulRequestsOpen] = React.useState(true);

    const invalidRequests = React.useMemo(() => failedRequests.filter(fr => fr.isEditable), [
        failedRequests,
    ]);
    const requestsWithError = React.useMemo(() => failedRequests.filter(fr => !fr.isEditable), [
        failedRequests,
    ]);

    const serviceNowPopoverRef = useServiceNowPopoverRef(requestsWithError, {
        placement: 'below',
        justify: 'end',
        centered: false,
    });

    return (
        <ModalSideSheet
            header="Saving requests"
            show={
                pendingRequests.length > 0 ||
                failedRequests.length > 0 ||
                successfulRequests.length > 0
            }
            onClose={onClose}
        >
            {invalidRequests.length > 0 && (
                <div className={styles.failedRequests}>
                    <div className={styles.header}>
                        <h3>Invalid requests</h3>
                        <Button>Edit failed</Button>
                    </div>
                    <div className={styles.progressList}>
                        {invalidRequests.map((request, index) => (
                            <InvalidRequestProgressItem
                                key={index.toString()}
                                request={request.item}
                                error={request.error}
                            />
                        ))}
                    </div>
                </div>
            )}
            {requestsWithError.length > 0 && (
                <div className={styles.failedRequests}>
                    <div className={styles.header}>
                        <h3>Failed requests</h3>
                        <p>
                            Open ticked for failed requests in{' '}
                            <a
                                href="#"
                                ref={serviceNowPopoverRef as React.RefObject<HTMLAnchorElement>}
                            >
                                Service Now
                            </a>
                        </p>
                    </div>
                    <div className={styles.progressList}>
                        {requestsWithError.map((request, index) => (
                            <FailedRequestProgressItem
                                key={index.toString()}
                                request={request.item}
                                error={request.error}
                            />
                        ))}
                    </div>
                </div>
            )}
            <Accordion>
                {pendingRequests.length > 0 && (
                    <AccordionItem
                        label={`In progress (${pendingRequests.length})`}
                        isOpen={isPendingRequestsOpen}
                        onChange={() => setIsPendingRequestsOpen(!isPendingRequestsOpen)}
                    >
                        <div className={styles.progressList}>
                            {pendingRequests.map((request, index) => (
                                <PendingRequestProgressItem
                                    key={index.toString()}
                                    request={request}
                                />
                            ))}
                        </div>
                    </AccordionItem>
                )}
                {successfulRequests.length > 0 && (
                    <AccordionItem
                        label={`Successful (${successfulRequests.length})`}
                        isOpen={isSuccessfulRequestsOpen}
                        onChange={() => setIsSuccessfulRequestsOpen(!isSuccessfulRequestsOpen)}
                    >
                        <div className={styles.progressList}>
                            {successfulRequests.map((request, index) => (
                                <SuccesfulRequestProgressItem
                                    key={index.toString()}
                                    request={request.item}
                                    response={request.response}
                                />
                            ))}
                        </div>
                    </AccordionItem>
                )}
            </Accordion>
        </ModalSideSheet>
    );
};

export default RequestProgressSidesheet;

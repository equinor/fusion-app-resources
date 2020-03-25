import * as React from 'react';
import {
    ModalSideSheet,
    Accordion,
    AccordionItem,
    Spinner,
    IconButton,
    CloseIcon,
    ErrorIcon,
    DoneIcon,
    Button,
} from '@equinor/fusion-components';
import useServiceNowPopoverRef from '../../hooks/useServiceNowPopoverRef';
import classNames from 'classnames';
import * as styles from './styles.less';
import { FusionApiHttpErrorResponse } from '@equinor/fusion';

export type FailedRequest<T> = {
    item: T;
    error: FusionApiHttpErrorResponse;
    isEditable: boolean;
};

export type SuccessfulRequest<T, TResponse> = {
    item: T;
    response: TResponse;
};

export type RenderRequestProps<TRequest> = {
    request: TRequest;
};

type RequestProgressSidesheetProps<TRequest, TResponse> = {
    pendingRequests: TRequest[];
    failedRequests: FailedRequest<TRequest>[];
    successfulRequests: SuccessfulRequest<TRequest, TResponse>[];
    renderRequest: React.FC<RenderRequestProps<TRequest>>;
    onClose: () => void;
};

type RequestItemProps<TRequest> = {
    request: TRequest;
    renderRequest: React.FC<RenderRequestProps<TRequest>>;
};

type FailedRequestItemProps<TRequest> = RequestItemProps<TRequest> & {
    error: FusionApiHttpErrorResponse;
};

type SuccessfulRequestItemProps<TRequest, TResponse> = RequestItemProps<TRequest> & {
    response: TResponse;
};

function PendingRequestProgressItem<TRequest>({
    request,
    renderRequest,
}: RequestItemProps<TRequest>) {
    return (
        <div className={classNames(styles.item, styles.pending)}>
            <div className={styles.icon}>
                <Spinner inline size={24} />
            </div>
            <div className={styles.content}>{renderRequest({ request })}</div>
        </div>
    );
}

function InvalidRequestProgressItem<TRequest>({
    request,
    renderRequest,
    error,
}: FailedRequestItemProps<TRequest>) {
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
            <div className={styles.content}>{renderRequest({ request })}</div>
            <div className={styles.errorMessage}>{error.error?.message}</div>
        </div>
    );
}

function FailedRequestProgressItem<TRequest>({
    request,
    renderRequest,
}: FailedRequestItemProps<TRequest>) {
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
            <div className={styles.content}>{renderRequest({ request })}</div>
        </div>
    );
}

function SuccesfulRequestProgressItem<TRequest, TResponse>({
    request,
    renderRequest,
}: SuccessfulRequestItemProps<TRequest, TResponse>) {
    return (
        <div className={classNames(styles.item, styles.successful)}>
            <div className={styles.icon}>
                <DoneIcon />
            </div>
            <div className={styles.content}>{renderRequest({ request })}</div>
        </div>
    );
}

function RequestProgressSidesheet<TRequest, TResponse>({
    pendingRequests,
    failedRequests,
    successfulRequests,
    renderRequest,
    onClose,
}: RequestProgressSidesheetProps<TRequest, TResponse>) {
    const [isPendingRequestsOpen, setIsPendingRequestsOpen] = React.useState(true);
    const [isSuccessfulRequestsOpen, setIsSuccessfulRequestsOpen] = React.useState(true);

    const invalidRequests = React.useMemo(() => failedRequests.filter(fr => fr.isEditable), [
        failedRequests,
    ]);
    const requestsWithError = React.useMemo(() => failedRequests.filter(fr => !fr.isEditable), [
        failedRequests,
    ]);

    const serviceNowPopoverRef = useServiceNowPopoverRef(
        { requestsWithError },
        {
            placement: 'below',
            justify: 'end',
            centered: false,
        }
    );

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
                                renderRequest={renderRequest}
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
                                renderRequest={renderRequest}
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
                                    renderRequest={renderRequest}
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
                                    renderRequest={renderRequest}
                                />
                            ))}
                        </div>
                    </AccordionItem>
                )}
            </Accordion>
        </ModalSideSheet>
    );
}

export default RequestProgressSidesheet;

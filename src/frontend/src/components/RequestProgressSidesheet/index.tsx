
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
    useTooltipRef,
} from '@equinor/fusion-components';
import useServiceNowPopoverRef from '../../hooks/useServiceNowPopoverRef';
import classNames from 'classnames';
import styles from './styles.less';
import RequestValidationError from '../../models/RequestValidationError';
import { FC, useState, useMemo, useEffect, useCallback, RefObject } from 'react';

export type FailedRequest<T> = {
    item: T;
    error: RequestValidationError;
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
    title: string;
    pendingRequests: TRequest[];
    failedRequests: FailedRequest<TRequest>[];
    successfulRequests: SuccessfulRequest<TRequest, TResponse>[];
    renderRequest: FC<RenderRequestProps<TRequest>>;
    onRemoveFailedRequest: (request: FailedRequest<TRequest>) => void;
    onClose: () => void;
};

type RequestItemProps<TRequest> = {
    request: TRequest;
    renderRequest: FC<RenderRequestProps<TRequest>>;
};

type FailedRequestItemProps<TRequest> = RequestItemProps<TRequest> & {
    error: RequestValidationError;
    onRemove: () => void;
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
    onRemove,
}: FailedRequestItemProps<TRequest>) {
    const ignoreTooltipRef = useTooltipRef('Ignore');

    const errors = Object.values(error.errors || []).reduce((all, e) => [...all, ...e], []);

    return (
        <div className={classNames(styles.item, styles.failed)}>
            <div className={styles.icon}>
                <IconButton onClick={onRemove} ref={ignoreTooltipRef}>
                    <CloseIcon />
                </IconButton>
            </div>
            <div className={styles.icon}>
                <ErrorIcon outline={false} />
            </div>
            <div className={styles.content}>{renderRequest({ request })}</div>
            <div className={styles.errorMessage}>
                <div className={styles.errorMessage}>{error.error?.message}</div>
                {errors.length > 0 && (
                    <ul>
                        {errors.map((errorMessage, i) => (
                            <li key={i}>{errorMessage}</li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
}

function FailedRequestProgressItem<TRequest>({
    request,
    renderRequest,
    onRemove,
}: FailedRequestItemProps<TRequest>) {
    const ignoreTooltipRef = useTooltipRef('Ignore');
    return (
        <div className={classNames(styles.item, styles.failed)}>
            <div className={styles.icon}>
                <IconButton onClick={onRemove} ref={ignoreTooltipRef}>
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
    title,
    pendingRequests,
    failedRequests,
    successfulRequests,
    renderRequest,
    onRemoveFailedRequest,
    onClose,
}: RequestProgressSidesheetProps<TRequest, TResponse>) {
    const [isPendingRequestsOpen, setIsPendingRequestsOpen] = useState(true);
    const [isSuccessfulRequestsOpen, setIsSuccessfulRequestsOpen] = useState(true);

    const invalidRequests = useMemo(() => failedRequests.filter((fr) => fr.isEditable), [
        failedRequests,
    ]);
    const requestsWithError = useMemo(() => failedRequests.filter((fr) => !fr.isEditable), [
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

    const [isShowing, setIsShowing] = useState(false);
    useEffect(() => {
        setIsShowing(
            pendingRequests.length > 0 || failedRequests.length > 0 || successfulRequests.length > 0
        );
    }, [pendingRequests, failedRequests, successfulRequests]);

    const closeSidesheet = useCallback(() => {
        setIsShowing(false);
        onClose();
    }, [onClose]);

    useEffect(() => {
        if (isShowing && failedRequests.length === 0) {
            closeSidesheet();
        }
    }, [failedRequests, closeSidesheet]);

    return (
        <ModalSideSheet
            header={title}
            show={isShowing}
            onClose={onClose}
            safeClose={pendingRequests.length > 0}
            safeCloseTitle={`
                Closing this sidesheet won't stop the saving routine, but you will not be able to verify the result. 
                Are you sure you want to close this sidesheet ? `}
            safeCloseCancelLabel={'Cancel'}
            safeCloseConfirmLabel={"I'm sure"}
        >
            {invalidRequests.length > 0 && (
                <div className={styles.failedRequests}>
                    <div className={styles.header}>
                        <h3>Invalid requests</h3>
                        <Button onClick={closeSidesheet}>Edit failed</Button>
                    </div>
                    <div className={styles.progressList}>
                        {invalidRequests.map((request, index) => (
                            <InvalidRequestProgressItem
                                key={index.toString()}
                                request={request.item}
                                error={request.error}
                                renderRequest={renderRequest}
                                onRemove={() => onRemoveFailedRequest(request)}
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
                                onClick={(e) => e.preventDefault()}
                                ref={serviceNowPopoverRef as RefObject<HTMLAnchorElement>}
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
                                onRemove={() => onRemoveFailedRequest(request)}
                            />
                        ))}
                    </div>
                </div>
            )}
            <div className={styles.accordionContainer}>
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
            </div>
        </ModalSideSheet>
    );
}

export default RequestProgressSidesheet;

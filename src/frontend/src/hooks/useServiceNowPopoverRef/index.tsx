
import useForm from '../useForm';
import {
    TextInput,
    TextArea,
    Button,
    usePopoverRef,
    ErrorMessage,
    Spinner,
    PopoverContainerProps,
} from '@equinor/fusion-components';
import { useNotificationCenter, useCurrentApp } from '@equinor/fusion';
import { ServiceNowIncidentRequest } from '../../api/ServiceNowApiClient';
import { useAppContext } from '../../appContext';
import styles from './styles.less';
import { FC, useCallback, useEffect, useState } from 'react';

const serializeLocalStorage = () => {
    const store: Record<string, string | null> = {};
    for (let i = 0, l = localStorage.length; i < l; i++) {
        const key = localStorage.key(i);
        if (key) {
            store[key] = localStorage.getItem(key);
        }
    }
    return store;
};

const createDefaultIncident = (): ServiceNowIncidentRequest => ({
    description: '',
    shortDescription: '',
    metadata: {
        url: window.location.href,
        currentApp: '',
        sessionId: '',
        browser: window.clientInformation.userAgent,
        timeZoneOffset: new Date().getTimezoneOffset(),
        localStorage: {},
        custom: {},
    },
});

type ServiceNowFormProps = {
    error: Error | null;
    onRetry: () => void;
    isShowing: boolean;
    onSubmit: (request: ServiceNowIncidentRequest) => void;
    onCancel: () => void;
    isSubmitting: boolean;
};

const ServiceNowForm: FC<ServiceNowFormProps> = ({
    error,
    onRetry,
    isShowing,
    onSubmit,
    onCancel,
    isSubmitting,
}) => {
    const { formState, formFieldSetter, resetForm } = useForm<ServiceNowIncidentRequest>(
        createDefaultIncident,
        () => true
    );

    const handleSubmit = useCallback(async () => {
        onSubmit(formState);
    }, [formState, onSubmit]);

    useEffect(() => {
        if (!isShowing) {
            resetForm();
        }
    }, [isShowing]);

    return (
        <div className={styles.container}>
            <h2>Create new incident</h2>
            <ErrorMessage
                hasError={error !== null}
                errorType="error"
                message={error?.message}
                onTakeAction={onRetry}
                action="Try again"
            >
                <div className={styles.row}>
                    <TextInput
                        label="Short description"
                        value={formState.shortDescription}
                        onChange={formFieldSetter('shortDescription')}
                    />
                </div>
                <div className={styles.row}>
                    <TextArea
                        label="Description"
                        value={formState.description}
                        onChange={formFieldSetter('description')}
                    />
                </div>
                <div className={styles.actions}>
                    <Button onClick={handleSubmit}>
                        {isSubmitting ? <Spinner inline /> : 'Submit'}
                    </Button>
                    <Button onClick={onCancel} outlined disabled={isSubmitting}>
                        Cancel
                    </Button>
                </div>
            </ErrorMessage>
        </div>
    );
};

const useServiceNowPopoverRef = (metadata: any | null, popoverProps?: PopoverContainerProps) => {
    const [shouldShow, setShouldShow] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    const { serviceNowApiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const currentApp = useCurrentApp();
    const handleSubmit = useCallback(
        async (request: ServiceNowIncidentRequest) => {
            setIsSubmitting(true);
            try {
                const requestWithMetadata = {
                    ...request,
                    url: window.location.href,
                    currentApp: currentApp?.name,
                    metadata: {
                        ...request.metadata,
                        localStorage: serializeLocalStorage(),
                        custom: metadata,
                    },
                };
                await serviceNowApiClient.createIncidentAsync(requestWithMetadata);
                sendNotification({
                    level: 'low',
                    title: 'Incident created',
                });
                setShouldShow(false);
            } catch (e) {
                setError(e);
            }
            setIsSubmitting(false);
        },
        [serviceNowApiClient, metadata]
    );

    const handleCancel = useCallback(() => {
        setShouldShow(false);
    }, []);

    const handleRetry = useCallback(() => {
        setError(null);
    }, []);

    const [popoverRef, isShowing, setIsShowing] = usePopoverRef(
        <ServiceNowForm
            error={error}
            onRetry={handleRetry}
            isShowing={shouldShow}
            onSubmit={handleSubmit}
            onCancel={handleCancel}
            isSubmitting={isSubmitting}
        />,
        popoverProps
    );

    useEffect(() => {
        setShouldShow(isShowing);
    }, [isShowing]);

    useEffect(() => {
        setIsShowing(shouldShow);
    }, [shouldShow]);

    return popoverRef;
};

export default useServiceNowPopoverRef;

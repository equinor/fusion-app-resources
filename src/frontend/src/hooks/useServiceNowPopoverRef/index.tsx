import * as React from 'react';
import useForm from '../useForm';
import {
    TextInput,
    TextArea,
    Button,
    usePopoverRef,
    ErrorMessage,
} from '@equinor/fusion-components';
import { useNotificationCenter, useCurrentApp, useTelemetryLogger } from '@equinor/fusion';
import { ServiceNowIncidentRequest } from '../../api/ServiceNowApiClient';
import { useAppContext } from '../../appContext';
import * as styles from './styles.less';
import { PopoverContainerProps } from '@equinor/fusion-components/dist/hooks/usePopoverRef/components/Container';

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
};

const ServiceNowForm: React.FC<ServiceNowFormProps> = ({
    error,
    onRetry,
    isShowing,
    onSubmit,
    onCancel,
}) => {
    const { formState, formFieldSetter, resetForm } = useForm<ServiceNowIncidentRequest>(
        createDefaultIncident,
        () => true
    );

    const handleSubmit = React.useCallback(async () => {
        onSubmit(formState);
    }, [formState, onSubmit]);

    React.useEffect(() => {
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
                    <Button onClick={handleSubmit}>Submit</Button>
                    <Button onClick={onCancel} outlined>
                        Cancel
                    </Button>
                </div>
            </ErrorMessage>
        </div>
    );
};

const useServiceNowPopoverRef = (metadata: any | null, popoverProps?: PopoverContainerProps) => {
    const [shouldShow, setShouldShow] = React.useState(false);
    const [error, setError] = React.useState<Error | null>(null);

    const { serviceNowApiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const currentApp = useCurrentApp();
    const handleSubmit = React.useCallback(
        async (request: ServiceNowIncidentRequest) => {
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
        },
        [serviceNowApiClient, metadata]
    );

    const handleCancel = React.useCallback(() => {
        setShouldShow(false);
    }, []);

    const handleRetry = React.useCallback(() => {
        setError(null);
    }, []);

    const [popoverRef, isShowing, setIsShowing] = usePopoverRef(
        <ServiceNowForm
            error={error}
            onRetry={handleRetry}
            isShowing={shouldShow}
            onSubmit={handleSubmit}
            onCancel={handleCancel}
        />,
        popoverProps
    );

    React.useEffect(() => {
        setShouldShow(isShowing);
    }, [isShowing]);

    React.useEffect(() => {
        setIsShowing(shouldShow);
    }, [shouldShow]);

    return popoverRef;
};

export default useServiceNowPopoverRef;

import * as React from 'react';
import { ErrorMessage, BlockIcon } from '@equinor/fusion-components';
import * as styles from './styles.less';
import ResourceError from '../../reducers/ResourceError';
import { ErrorTypes } from '@equinor/fusion-components/dist/components/general/ErrorMessage';
import classNames from 'classnames';
import { useComponentDisplayClassNames } from '@equinor/fusion';
import { styling } from '@equinor/fusion-components';

const iconProps = {
    width: 80,
    height: 80,
    cursor: 'default',
    color: styling.colors.blackAlt2,
};

type AccessDeniedProps = { error: ResourceError };
const AccessDenied: React.FC<AccessDeniedProps> = ({ error }) => {
    const { accessRequirements, message } = error.response.error;

    const messageContainerClasses = classNames(
        styles.messageContainer,
        useComponentDisplayClassNames(styles)
    );

    return (
        <div className={styles.container}>
            <div className={messageContainerClasses}>
                <BlockIcon {...iconProps} />
                <div className={styles.title}>{message}</div>
                {accessRequirements && accessRequirements.length > 0 && (
                    <div className={styles.requirementsContainer}>
                        <div className={styles.message}>
                            The reason you can not access the data is the following
                        </div>
                        <ul>
                            {accessRequirements.map((req, i) => (
                                <li key={req.code + i} className={styles.requirement}>
                                    <div className={styles.outcome}>{req.outcome}</div>
                                    <div className={styles.description}>{req.description}</div>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}
            </div>
        </div>
    );
};

const getErrorType = (error?: number): ErrorTypes => {
    switch (error) {
        case 403:
        case 401:
            return 'accessDenied';
        default:
            return 'error';
    }
};

type ResourceErrorMessageProps = {
    error: ResourceError | null;
};
const ResourceErrorMessage: React.FC<ResourceErrorMessageProps> = ({ error, children }) => {
    if (error && (error.statusCode === 403 || error.statusCode === 401)) {
        return <AccessDenied error={error} />;
    }

    return (
        <ErrorMessage
            hasError={Boolean(error)}
            errorType={getErrorType(error?.statusCode)}
            resourceName="contracts"
            title={error?.response.error.message}
        >
            {children}
        </ErrorMessage>
    );
};

export default ResourceErrorMessage;

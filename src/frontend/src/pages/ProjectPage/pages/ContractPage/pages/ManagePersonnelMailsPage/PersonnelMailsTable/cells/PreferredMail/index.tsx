import { CloseCircleIcon, styling, useTooltipRef } from '@equinor/fusion-components';
import { ChangeEvent, FC, useCallback, useState } from 'react';
import Personnel from '../../../../../../../../../models/Personnel';
import { useManagePersonnelMailContext } from '../../../ManagePersonnelMailContext';
import * as styles from './styles.less';

type PreferredMailProps = {
    item: Personnel;
};

const emailValidationRegex = /\S+@\S+\.\S+/;

const PreferredMail: FC<PreferredMailProps> = ({ item }) => {
    const [input, setInput] = useState<string>(item.preferredContactMail || '');
    const [validationError, setHasValidationError] = useState<boolean>(false);
    const { updateContactMail } = useManagePersonnelMailContext();

    const invalidMailTooltip = useTooltipRef('Invalid mail');

    const onPreferredMailChange = useCallback(
        (value: ChangeEvent<HTMLInputElement>) => {
            setInput(value.target.value);
        },
        [setInput]
    );

    const validateInput = useCallback(() => {
        const isValid =
            input.length === 0 || emailValidationRegex.test(String(input).toLowerCase());
        setHasValidationError(!isValid);
        return isValid;
    }, [input]);

    const onBlur = useCallback(() => {
        const isValid = validateInput();
        isValid && updateContactMail(item.personnelId, input);
    }, [input, updateContactMail]);

    return (
        <div className={styles.container}>
            <input
                className={styles.mailInput}
                value={input}
                onChange={onPreferredMailChange}
                onBlur={onBlur}
            />
            <div className={styles.errorContainer}>
                {validationError && (
                    <div className={styles.error} ref={invalidMailTooltip}>
                        <CloseCircleIcon color={styling.colors.red} />
                    </div>
                )}
            </div>
        </div>
    );
};

export default PreferredMail;

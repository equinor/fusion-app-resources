import { FC, useEffect } from 'react';
import { registerApp, useCurrentUser, useNotificationCenter } from '@equinor/fusion';
import { Button, usePopoverRef } from '@equinor/fusion-components';

import * as styles from './styles.less';

const App: FC = () => {
    const currentUser = useCurrentUser();
    const sendNotification = useNotificationCenter();

    const [popoverRef] = usePopoverRef(
        <div className={styles.popover}>What a lovely popover 💩</div>,
        {
            placement: 'below',
        }
    );
    
    const sendWelcomeNotification = async () => {
        await sendNotification({
            id: 'This is a unique id which means the notification will only be shown once',
            level: 'medium',
            title:
                'Welcome to your new fusion app! Open up src/index.tsx to start building your app!',
        });
    };

    useEffect(() => {
        sendWelcomeNotification();
    }, []);

    if (!currentUser) {
        return null;
    }

    return (
        <div className={styles.hello}>
            <h1>Oh hello there, {currentUser.fullName}</h1>
            <Button ref={popoverRef}>Click me!</Button>
        </div>
    );
};

registerApp('{appKey}', {
    AppComponent: App,
});

if (module.hot) {
    module.hot.accept();
}
